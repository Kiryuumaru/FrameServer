using Application.Common.Extensions;
using Application.Common.Features;
using Application.Configuration.Services;
using Application.FrameStreamer.Features;
using DisposableHelpers.Attributes;
using Domain.Events;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.FrameStreamer.Workers;

public class FrameStreamerWorker(ILogger<FrameStreamerWorker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private class FrameSourceRuntimeLifetime(FrameSourceRuntime frameSourceRuntime)
    {
        public static FrameSourceRuntimeLifetime Create(FrameSourceConfig frameSourceConfig)
        {
            return new FrameSourceRuntimeLifetime(FrameSourceRuntime.Create(frameSourceConfig));
        }

        public FrameSourceRuntime FrameSourceRuntime { get; } = frameSourceRuntime;

        public GateKeeper LifetimeGate { get; } = new(true);

        public async Task Destroy(CancellationToken cancellationToken)
        {
            await FrameSourceRuntime.DisposeAsync();
            await LifetimeGate.WaitForClosed(cancellationToken);
        }
    }

    private readonly TimeSpan _openFrameSourceTimeout = TimeSpan.FromSeconds(30);

    private readonly ILogger<FrameStreamerWorker> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private readonly Locker _locker = new();
    private readonly Dictionary<string, FrameSourceRuntimeLifetime> _sourceFlatMap = [];
    private readonly Dictionary<int, FrameSourceRuntimeLifetime> _portFlatMap = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ExecuteAsyncAndForget(stoppingToken);
        return Task.CompletedTask;
    }

    protected async void ExecuteAsyncAndForget(CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(ExecuteAsyncAndForget));

        Cv2.SetLogLevel(OpenCvSharp.LogLevel.SILENT);
        Cv2.SetBreakOnError(false);

        var frameSourceConfigurationService = _serviceProvider.GetRequiredService<FrameSourceConfigurationService>();

        frameSourceConfigurationService.GetAllConfig();

        var frameSourceAddedCallbackToken = await frameSourceConfigurationService.SubscribeFrameSourceAddedCallback(FrameSourceAddedCallback, stoppingToken);
        var frameSourceModifiedCallbackToken = await frameSourceConfigurationService.SubscribeFrameSourceModifiedCallback(FrameSourceModifiedCallback, stoppingToken);
        var frameSourceRemovedCallbackToken = await frameSourceConfigurationService.SubscribeFrameSourceRemovedCallback(FrameSourceRemovedCallback, stoppingToken);

        stoppingToken.Register(() =>
        {
            frameSourceAddedCallbackToken.Dispose();
            frameSourceModifiedCallbackToken.Dispose();
            frameSourceRemovedCallbackToken.Dispose();
        });
    }

    private async Task FrameSourceAddedCallback(FrameSourceAddedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(FrameSourceAddedCallback));
        using var lockObj = await _locker.WaitAsync(cancellationToken);

        if (_sourceFlatMap.TryGetValue(eventArgs.Key, out var frameSourceStream))
        {
            _logger.LogDebug("Destroying old frame source {FrameSourceKey}", eventArgs.Key);
            await frameSourceStream.Destroy(cancellationToken);
            _sourceFlatMap.Remove(eventArgs.Key);
        }

        _logger.LogDebug("Adding frame source {FrameSourceKey}", eventArgs.Key);
        frameSourceStream = FrameSourceRuntimeLifetime.Create(eventArgs.NewConfig);
        _sourceFlatMap.Add(eventArgs.Key, frameSourceStream);

        if (eventArgs.NewConfig.Enabled)
        {
            _logger.LogDebug("Enabling frame source {FrameSourceKey}", eventArgs.Key);
            StartSource(frameSourceStream, cancellationToken).Forget();
        }
        else
        {
            _logger.LogDebug("Disabling frame source {FrameSourceKey}", eventArgs.Key);
            frameSourceStream.LifetimeGate.SetClosed();
        }
    }

    private async Task FrameSourceModifiedCallback(FrameSourceModifiedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(FrameSourceModifiedCallback));
        using var lockObj = await _locker.WaitAsync(cancellationToken);

        if (_sourceFlatMap.TryGetValue(eventArgs.Key, out var frameSourceStream))
        {
            _logger.LogDebug("Destroying old frame source {FrameSourceKey}", eventArgs.Key);
            await frameSourceStream.Destroy(cancellationToken);
            _sourceFlatMap.Remove(eventArgs.Key);
        }

        _logger.LogDebug("Re-adding frame source {FrameSourceKey}", eventArgs.Key);
        frameSourceStream = FrameSourceRuntimeLifetime.Create(eventArgs.NewConfig);
        _sourceFlatMap.Add(eventArgs.Key, frameSourceStream);

        if (eventArgs.NewConfig.Enabled)
        {
            _logger.LogDebug("Enabling frame source {FrameSourceKey}", eventArgs.Key);
            StartSource(frameSourceStream, cancellationToken).Forget();
        }
        else
        {
            _logger.LogDebug("Disabling frame source {FrameSourceKey}", eventArgs.Key);
            frameSourceStream.LifetimeGate.SetClosed();
        }
    }

    private async Task FrameSourceRemovedCallback(FrameSourceRemovedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(FrameSourceRemovedCallback));
        using var lockObj = await _locker.WaitAsync(cancellationToken);

        if (_sourceFlatMap.TryGetValue(eventArgs.Key, out var frameSourceStream))
        {
            _logger.LogDebug("Destroying old frame source {FrameSourceKey}", eventArgs.Key);
            await frameSourceStream.Destroy(cancellationToken);
            _sourceFlatMap.Remove(eventArgs.Key);
        }
    }

    private async Task StartSource(FrameSourceRuntimeLifetime frameSourceRuntimeLifetime, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(StartSource), new Dictionary<string, object?>
        {
            ["FrameSourceConfig"] = frameSourceRuntimeLifetime.FrameSourceRuntime.FrameSourceConfig
        });

        FrameSourceRuntime frameSourceRuntime = frameSourceRuntimeLifetime.FrameSourceRuntime;
        FrameSourceConfig frameSourceConfig = frameSourceRuntime.FrameSourceConfig;
        string source = frameSourceRuntimeLifetime.FrameSourceRuntime.FrameSourceConfig.Source;

        bool isRunning() => !cancellationToken.IsCancellationRequested && !frameSourceRuntime.IsDisposedOrDisposing;

        while (isRunning())
        {
            while (isRunning())
            {
                try
                {
                    _logger.LogInformation("Opening frame source '{FrameSource}'", source);

                    await frameSourceRuntime.Open(cancellationToken.WithTimeout(_openFrameSourceTimeout));

                    break;
                }
                catch (Exception ex)
                {
                    if (!isRunning()) break;

                    _logger.LogError("Failed to open frame source '{FrameSource}': {ErrorMessage}, Retrying in 5 seconds...", source, ex.Message);

                    await TaskUtils.DelayAndForget(5000, cancellationToken);
                }
            }

            if (!isRunning()) break;

            try
            {
                _logger.LogInformation("Starting frame source '{FrameSource}'", source);

                await frameSourceRuntime.Start(cancellationToken);

                if (!isRunning()) break;

                _logger.LogInformation("Frame source '{FrameSource}' ended. Starting again in 5 seconds...", source);
            }
            catch (Exception ex)
            {
                if (!isRunning()) break;

                _logger.LogError("Failed to start frame source '{FrameSource}': {ErrorMessage}, Retrying in 5 seconds...", source, ex.Message);
            }

            await TaskUtils.DelayAndForget(5000, cancellationToken);
        }

        if (frameSourceConfig.ShowWindow)
        {
            Cv2.DestroyWindow($"Frame Server Source {source}");
        }

        InvokerUtils.RunAndForget(frameSourceRuntime.Dispose);

        await TaskUtils.DelayAndForget(1000, cancellationToken);

        frameSourceRuntimeLifetime.LifetimeGate.SetClosed();

        _logger.LogInformation("Frame source '{FrameSource}' terminated", source);
    }
}
