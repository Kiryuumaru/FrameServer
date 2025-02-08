using Application.Common.Extensions;
using Application.Common.Features;
using Application.Configuration.Services;
using Domain.Events;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.FrameStreamer.Workers;

public class FrameStreamerWorker(ILogger<FrameStreamerWorker> logger, IServiceProvider serviceProvider) : BackgroundService
{
    private class FrameSourceRuntime
    {
        public Locker Locker { get; } = new();

        public required FrameSourceConfig FrameSourceConfig { get; set; }

        public required VideoCapture VideoCapture { get; set; }
    }

    private readonly ILogger<FrameStreamerWorker> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private readonly Locker _locker = new();
    private readonly Dictionary<string, FrameSourceRuntime> _nameFlatMap = [];
    private readonly Dictionary<string, FrameSourceRuntime> _sourceFlatMap = [];
    private readonly Dictionary<int, FrameSourceRuntime> _portFlatMap = [];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ExecuteAsyncAndForget(stoppingToken);
        return Task.CompletedTask;
    }

    protected async void ExecuteAsyncAndForget(CancellationToken stoppingToken)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(ExecuteAsyncAndForget));

        Cv2.SetLogLevel(OpenCvSharp.LogLevel.SILENT);
        Cv2.SetBreakOnError(true);

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

    private void FrameSourceAddedCallback(FrameSourceAddedEventArgs eventArgs)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(FrameSourceAddedCallback));

        StartSource(eventArgs.NewConfig).Forget();

        _logger.LogInformation("Frame source added: Key={Key}, Source={Source}, Port={Port}, Enabled={Enabled}, Height={Height}, Width={Width}",
            eventArgs.Key, eventArgs.NewConfig.Source, eventArgs.NewConfig.Port, eventArgs.NewConfig.Enabled, eventArgs.NewConfig.Height, eventArgs.NewConfig.Width);
    }

    private void FrameSourceModifiedCallback(FrameSourceModifiedEventArgs eventArgs)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(FrameSourceModifiedCallback));

        _logger.LogInformation("Frame source modified: Key={Key}, OldSource={OldSource}, OldPort={OldPort}, OldEnabled={OldEnabled}, OldHeight={OldHeight}, OldWidth={OldWidth}, NewSource={NewSource}, NewPort={NewPort}, NewEnabled={NewEnabled}, NewHeight={NewHeight}, NewWidth={NewWidth}",
            eventArgs.Key, eventArgs.OldConfig.Source, eventArgs.OldConfig.Port, eventArgs.OldConfig.Enabled, eventArgs.OldConfig.Height, eventArgs.OldConfig.Width,
            eventArgs.NewConfig.Source, eventArgs.NewConfig.Port, eventArgs.NewConfig.Enabled, eventArgs.NewConfig.Height, eventArgs.NewConfig.Width);
    }

    private void FrameSourceRemovedCallback(FrameSourceRemovedEventArgs eventArgs)
    {
        using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(FrameSourceRemovedCallback));

        _logger.LogInformation("Frame source removed: Key={Key}, Source={Source}, Port={Port}, Enabled={Enabled}, Height={Height}, Width={Width}",
            eventArgs.Key, eventArgs.OldConfig.Source, eventArgs.OldConfig.Port, eventArgs.OldConfig.Enabled, eventArgs.OldConfig.Height, eventArgs.OldConfig.Width);
    }

    private Task StartSource(FrameSourceConfig frameSourceConfig)
    {
        return ThreadHelpers.WaitThread(() =>
        {
            using var _ = _logger.BeginScopeMap(nameof(FrameStreamerWorker), nameof(StartSource), new Dictionary<string, object?>
            {
                ["FrameSourceConfig"] = frameSourceConfig
            });

            try
            {
                VideoCapture videoCapture = new();
                videoCapture.SetExceptionMode(true);
                VideoCaptureAPIs videoCaptureAPIs = VideoCaptureAPIs.ANY;
                if (!string.IsNullOrEmpty(frameSourceConfig.VideoApi))
                {
                    videoCaptureAPIs = OpenCVExtensions.StringToVideoCaptureApi(frameSourceConfig.VideoApi);
                }
                bool isOpen = false;
                if (int.TryParse(frameSourceConfig.Source, out int cameraSource))
                {
                    isOpen = videoCapture.Open(cameraSource, videoCaptureAPIs);
                }
                else
                {
                    isOpen = videoCapture.Open(frameSourceConfig.Source, videoCaptureAPIs);
                }

                if (!isOpen)
                {
                    throw new Exception($"Failed to open frame source '{frameSourceConfig.Source}'");
                }

                Mat frame = new();
                while (videoCapture.IsOpened())
                {
                    var hasRead = videoCapture.Read(frame);

                    if (hasRead && !frame.Empty())
                    {
                        Cv2.ImShow("Frame", frame);
                    }

                    Cv2.WaitKey(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error on start frame source: {ErrorMessage}", ex.Message);
            }
        });
    }
}
