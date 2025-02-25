using Domain.Events;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Application.Configuration.Extensions;
using Microsoft.Extensions.Hosting;
using AbsolutePathHelpers;
using OpenCvSharp;
using Application.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Application.Configuration.Services;
using System.Threading;
using Application.Common.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Application.Configuration.Models;

namespace Application.Configuration.Workers;

public class FrameSourceConfigurationWorker(ILogger<FrameSourceConfigurationWorker> logger, IServiceProvider serviceProvider, IConfiguration configuration) : BackgroundService
{
    private readonly ILogger<FrameSourceConfigurationWorker> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IConfiguration _configuration = configuration;

    private readonly Locker _loadLocker = new();
    private readonly Dictionary<string, FrameSourceConfig> _frameSources = [];

    private FileSystemWatcher? _fileWatcher;

    public Dictionary<string, FrameSourceConfig>? GetAllConfig()
    {
        return new Dictionary<string, FrameSourceConfig>(_frameSources);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ExecuteAsyncAndForget(stoppingToken);
        return Task.CompletedTask;
    }

    protected async void ExecuteAsyncAndForget(CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameSourceConfigurationWorker), nameof(ExecuteAsyncAndForget));

        await Task.Delay(2000, stoppingToken);

        var directory = _configuration.GetHomePath();
        var configFileFilter = _configuration.GetConfigFileFilter();

        directory.CreateDirectory();

        _fileWatcher = new FileSystemWatcher(directory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
        };

        stoppingToken.Register(_fileWatcher.Dispose);

        foreach (var f in FixConfigFilter(configFileFilter))
        {
            _fileWatcher.Filters.Add(f);
        }

        _fileWatcher.Changed += OnConfigFileChanged;
        _fileWatcher.Created += OnConfigFileChanged;
        _fileWatcher.Deleted += OnConfigFileChanged;
        _fileWatcher.Renamed += OnConfigFileChanged;
        _fileWatcher.EnableRaisingEvents = true;

        _logger.LogInformation("Monitoring '{ConfigFilter}' for changes...", Path.Combine(directory, configFileFilter));

        await LoadConfiguration(stoppingToken);
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Task.Run(async () =>
        {
            using var _ = _logger.BeginScopeMap(nameof(FrameSourceConfigurationWorker), nameof(OnConfigFileChanged));

            await Task.Delay(500);
            try
            {
                await LoadConfiguration(default);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reloading configuration: {ErrorMessage}", ex.Message);
            }
        });
    }

    private async Task LoadConfiguration(CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameSourceConfigurationWorker), nameof(LoadConfiguration));

        var frameSourceConfigurationService = _serviceProvider.GetRequiredService<FrameSourceConfigurationHolderService>();

        string directory = _configuration.GetHomePath();
        string configFileFilter = _configuration.GetConfigFileFilter();
        IDeserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        try
        {
            using var configLock = await _loadLocker.WaitAsync(cancellationToken);

            var files = GetAllFilesUsingFilter(directory, configFileFilter);
            var newFrameSources = new Dictionary<string, (AbsolutePath Path, FrameSourceConfig Config)>();

            foreach (var file in files)
            {
                using var reader = new StreamReader(file);
                try
                {
                    var fileConfig = _deserializer.Deserialize<FrameServerConfigFile>(reader);
                    if (fileConfig != null)
                    {
                        foreach (var kvp in fileConfig.Sources)
                        {
                            newFrameSources[kvp.Key] = (file, kvp.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error reading config '{file}': {ex.Message}");
                }
            }

            foreach (var kvp in newFrameSources)
            {
                if (string.IsNullOrEmpty(kvp.Value.Config.Source))
                {
                    throw new Exception($"Source is missing for key '{kvp.Key}' in file '{kvp.Value.Path}'");
                }
                if (kvp.Value.Config.Port < 1 || kvp.Value.Config.Port > 65535)
                {
                    throw new Exception($"Port must be between 1 and 65535 for key '{kvp.Key}' in file '{kvp.Value.Path}'");
                }
            }

            foreach (var kvp in newFrameSources)
            {
                if (!string.IsNullOrEmpty(kvp.Value.Config.VideoApi))
                {
                    OpenCVExtensions.StringToVideoCaptureApi(kvp.Value.Config.VideoApi);
                }
            }

            foreach (var kvp in newFrameSources)
            {
                var duplicateSource = newFrameSources.FirstOrDefault(i => i.Key != kvp.Key && i.Value.Config.Source.Equals(kvp.Value.Config.Source, StringComparison.InvariantCultureIgnoreCase));
                var duplicatePort = newFrameSources.FirstOrDefault(i => i.Key != kvp.Key && i.Value.Config.Port == kvp.Value.Config.Port);
                if (duplicateSource.Key != null)
                {
                    throw new Exception($"Duplicate source found for " +
                        $"'{kvp.Value.Path}' key '{kvp.Key}' and '{duplicateSource.Value.Path}' key '{duplicateSource.Key}': " +
                        $"Source: {kvp.Value.Config.Source}");
                }
                if (duplicatePort.Key != null)
                {
                    throw new Exception($"Duplicate port found for " +
                        $"'{kvp.Value.Path}' key '{kvp.Key}' and '{duplicatePort.Value.Path}' key '{duplicatePort.Key}': " +
                        $"Port: {kvp.Value.Config.Port}");
                }
            }

            var oldFrameSources = new Dictionary<string, FrameSourceConfig>(_frameSources);

            foreach (var oldEntry in oldFrameSources)
            {
                string resolution = GetStringResolution(oldEntry.Value);
                if (!newFrameSources.ContainsKey(oldEntry.Key))
                {
                    _frameSources.Remove(oldEntry.Key);
                    _logger.LogDebug("Frame source config removed: {RemovedFrameSourceKey}", oldEntry.Key);
                    _logger.LogDebug("Removed config '{RemovedFrameSourceKey}': {@RemovedFrameSourceConfig}", oldEntry.Key, oldEntry.Value);
                    await frameSourceConfigurationService.InvokeFrameSourceRemovedCallback(new FrameSourceRemovedEventArgs(oldEntry.Key, oldEntry.Value), cancellationToken);
                }
            }

            foreach (var newEntry in newFrameSources)
            {
                string resolution = GetStringResolution(newEntry.Value.Config);
                if (!oldFrameSources.TryGetValue(newEntry.Key, out var oldConfig))
                {
                    _frameSources[newEntry.Key] = newEntry.Value.Config;
                    _logger.LogInformation("Frame source config added: {AddedFrameSourceKey}", newEntry.Key);
                    _logger.LogDebug("Added config '{AddedFrameSourceKey}': {@AddedFrameSourceConfig}", newEntry.Key, newEntry.Value.Config);
                    await frameSourceConfigurationService.InvokeFrameSourceAddedCallback(new FrameSourceAddedEventArgs(newEntry.Key, newEntry.Value.Config), cancellationToken);
                }
                else
                {
                    if (oldConfig != newEntry.Value.Config)
                    {
                        _frameSources[newEntry.Key] = newEntry.Value.Config;
                        _logger.LogInformation("Frame source config modified: {ModifiedFrameSourceKey}", newEntry.Key);
                        _logger.LogDebug("Modified config '{ModifiedFrameSourceKey}' Old: {@OldModifiedFrameSourceConfig}, New: {@NewModifiedFrameSourceConfig}", newEntry.Key, oldConfig, newEntry.Value.Config);
                        await frameSourceConfigurationService.InvokeFrameSourceModifiedCallback(new FrameSourceModifiedEventArgs(newEntry.Key, oldConfig, newEntry.Value.Config), cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error on loading configs: {ErrorMessage}", ex.Message);
        }
    }

    private static string GetStringResolution(FrameSourceConfig frameSourceConfig)
    {
        return (frameSourceConfig.Width.HasValue && frameSourceConfig.Height.HasValue)
            ? $"{frameSourceConfig.Width}x{frameSourceConfig.Height}"
            : ((!frameSourceConfig.Width.HasValue && !frameSourceConfig.Height.HasValue)
                ? "default"
                : $"{(frameSourceConfig.Width.HasValue ? frameSourceConfig.Width : "default")}x{(frameSourceConfig.Height.HasValue ? frameSourceConfig.Height : "default")}");
    }

    private static List<string> FixConfigFilter(string filter)
    {
        List<string> filters = [];

        string filename = Path.GetFileName(filter);
        string filenameWithoutExtension = Path.GetFileNameWithoutExtension(filter);

        string ext = Path.GetExtension(filter);
        if (ext.Equals(".yml", StringComparison.InvariantCultureIgnoreCase) || ext.Equals(".yaml", StringComparison.InvariantCultureIgnoreCase))
        {
            filters.Add($"{filenameWithoutExtension}.yaml");
            filters.Add($"{filenameWithoutExtension}.yml");
        }
        else
        {
            filters.Add(filename);
        }

        return filters;
    }

    private static List<AbsolutePath> GetAllFilesUsingFilter(string directory, string filter)
    {
        var filters = FixConfigFilter(filter);
        var files = new List<AbsolutePath>();

        foreach (var f in filters)
        {
            files.AddRange(Directory.GetFiles(directory, f).Select(i => AbsolutePath.Create(i)));
        }

        return files;
    }
}
