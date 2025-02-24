using Application.Common.Extensions;
using Application.Common.Features;
using DisposableHelpers.Attributes;
using Domain.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.FrameStreamer.Features;

[Disposable]
public partial class FrameSourceRuntime
{
    public static FrameSourceRuntime Create(FrameSourceConfig frameSourceConfig)
    {
        var frameSourceRuntime = new FrameSourceRuntime()
        {
            FrameSourceConfig = frameSourceConfig
        };

        frameSourceRuntime._frameSourceStream.SetFrameCallback(frameSourceRuntime._frame, frameSourceRuntime.FrameCallback);
        frameSourceRuntime.CancelWhenDisposing();

        return frameSourceRuntime;
    }

    private readonly FrameSourceStream _frameSourceStream = new();
    private readonly Locker _locker = new();
    private readonly Locker _callbackLocker = new();
    private readonly Mat _frame = new();

    public required FrameSourceConfig FrameSourceConfig { get; init; }

    private async Task FrameCallback(CancellationToken cancellationToken)
    {
        if (!IsRunning()) return;

        using var callbackLock = await _callbackLocker.WaitAsync(default);

        if (!IsRunning()) return;

        //var ss = _frame.ToBytes(ext: ".png");
        if (FrameSourceConfig.ShowWindow)
        {
            Cv2.ImShow($"Frame Server Source {FrameSourceConfig.Source}", _frame);
        }
    }

    public async Task Open(CancellationToken cancellationToken)
    {
        using var _ = await _locker.WaitAsync(cancellationToken);
        await _frameSourceStream.Open(FrameSourceConfig, cancellationToken);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        using var _ = await _locker.WaitAsync(cancellationToken);
        await _frameSourceStream.Start(cancellationToken);
    }

    protected async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await _frameSourceStream.DisposeAsync();

            using var callbackLocker = await _callbackLocker.WaitAsync(default);
            using var locker = await _locker.WaitAsync(default);

            if (FrameSourceConfig.ShowWindow)
            {
                Cv2.DestroyWindow($"Frame Server Source {FrameSourceConfig.Source}");
            }

            InvokerUtils.RunAndForget(_frame.Release);
            InvokerUtils.RunAndForget(_frame.Dispose);
        }
    }

    private bool IsRunning() =>
        FrameSourceConfig != null &&
        !IsDisposedOrDisposing &&
        !_frameSourceStream.VideoCapture.IsDisposed &&
        _frameSourceStream.VideoCapture.IsOpened();
}
