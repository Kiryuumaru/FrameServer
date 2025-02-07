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

namespace Application.Configuration.Workers;

public class FrameSourceConfigurationWorkers(ILogger<FrameSourceConfigurationWorkers> logger, IServiceProvider serviceProvider, IConfiguration configuration) : BackgroundService
{
    private readonly ILogger<FrameSourceConfigurationWorkers> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IConfiguration _configuration = configuration;

    private readonly SemaphoreSlim _loadLocker = new(1);
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    private readonly Dictionary<string, FrameSourceConfig> _frameSources = [];

    private FileSystemWatcher? _fileWatcher;

    public Dictionary<string, FrameSourceConfig>? GetAllConfig()
    {
        return new Dictionary<string, FrameSourceConfig>(_frameSources);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameSourceConfigurationWorkers), nameof(ExecuteAsync));

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

        await LoadConfiguration();
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        Task.Run(async () =>
        {
            await Task.Delay(500);
            try
            {
                await LoadConfiguration();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reloading configuration: {ErrorMessage}", ex.Message);
            }
        });
    }

    private async Task LoadConfiguration()
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameSourceConfigurationWorkers), nameof(LoadConfiguration));

        var frameSourceConfigurationService = _serviceProvider.GetRequiredService<FrameSourceConfigurationService>();

        string directory = _configuration.GetHomePath();
        string configFileFilter = _configuration.GetConfigFileFilter();

        try
        {
            await _loadLocker.WaitAsync();

            var files = GetAllFilesUsingFilter(directory, configFileFilter);
            var newFrameSources = new Dictionary<string, (AbsolutePath Path, FrameSourceConfig Config)>();

            foreach (var file in files)
            {
                using var reader = new StreamReader(file);
                try
                {
                    var fileConfig = _deserializer.Deserialize<Dictionary<string, FrameSourceConfig>>(reader);
                    if (fileConfig != null)
                    {
                        foreach (var kvp in fileConfig)
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
                var duplicateSource = newFrameSources.FirstOrDefault(i => i.Key != kvp.Key && i.Value.Config.Source.Equals(kvp.Value.Config.Source, StringComparison.InvariantCultureIgnoreCase));
                var duplicatePort = newFrameSources.FirstOrDefault(i => i.Key != kvp.Key && i.Value.Config.Port == kvp.Value.Config.Port);
                if (duplicateSource.Key != null)
                {
                    throw new Exception($"Duplicate source found for '{kvp.Value.Path}' key '{kvp.Key}' and '{duplicateSource.Value.Path}' key '{duplicateSource.Key}': Source: {kvp.Value.Config.Source}");
                }
                if (duplicatePort.Key != null)
                {
                    throw new Exception($"Duplicate port found for '{kvp.Value.Path}' key '{kvp.Key}' and '{duplicatePort.Value.Path}' key '{duplicatePort.Key}': Port: {kvp.Value.Config.Port}");
                }
            }

            var oldFrameSources = new Dictionary<string, FrameSourceConfig>(_frameSources);

            foreach (var oldEntry in oldFrameSources)
            {
                if (!newFrameSources.ContainsKey(oldEntry.Key))
                {
                    _frameSources.Remove(oldEntry.Key);
                    _logger.LogInformation("Frame source config removed: {RemovedFrameSourceKey}", oldEntry.Key);
                    frameSourceConfigurationService.InvokeFrameSourceRemovedCallback(new FrameSourceRemovedEventArgs(oldEntry.Key, oldEntry.Value));
                }
            }

            foreach (var newEntry in newFrameSources)
            {
                string resolution = (newEntry.Value.Config.Width.HasValue && newEntry.Value.Config.Height.HasValue)
                    ? $"{newEntry.Value.Config.Width}x{newEntry.Value.Config.Height}"
                    : ((!newEntry.Value.Config.Width.HasValue && !newEntry.Value.Config.Height.HasValue)
                        ? "default"
                        : $"{(newEntry.Value.Config.Width.HasValue ? newEntry.Value.Config.Width : "default")}x{(newEntry.Value.Config.Height.HasValue ? newEntry.Value.Config.Height : "default")}");
                if (!oldFrameSources.TryGetValue(newEntry.Key, out var oldConfig))
                {
                    _frameSources[newEntry.Key] = newEntry.Value.Config;
                    _logger.LogInformation("Frame source config added '{AddedFrameSourceKey}': Source: {Source}, Port: {Port}, Resolution: {Resolution}", newEntry.Key, newEntry.Value.Config.Source, newEntry.Value.Config.Port, resolution);
                    frameSourceConfigurationService.InvokeFrameSourceAddedCallback(new FrameSourceAddedEventArgs(newEntry.Key, newEntry.Value.Config));
                }
                else
                {
                    if (oldConfig != newEntry.Value.Config)
                    {
                        _frameSources[newEntry.Key] = newEntry.Value.Config;
                        _logger.LogInformation("Frame source config modified '{ModifiedFrameSourceKey}': Source: {Source}, Port: {Port}, Resolution: {Resolution}", newEntry.Key, newEntry.Value.Config.Source, newEntry.Value.Config.Port, resolution);
                        frameSourceConfigurationService.InvokeFrameSourceModifiedCallback(new FrameSourceModifiedEventArgs(newEntry.Key, oldConfig, newEntry.Value.Config));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error on loading configs: {ErrorMessage}", ex.Message);
        }
        finally
        {
            _loadLocker.Release();
        }
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
