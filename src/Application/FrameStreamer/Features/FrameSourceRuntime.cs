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
    public static FrameSourceRuntime Start(FrameSourceConfig frameSourceConfig)
    {
        var frameSourceRuntime = new FrameSourceRuntime(frameSourceConfig);

        frameSourceRuntime.Start();

        return frameSourceRuntime;
    }

    private readonly FrameSourceConfig _frameSourceConfig;
    private readonly FrameSourceStream _frameSourceStream = new();
    private readonly Locker _locker = new();
    private readonly Locker _callbackLocker = new();
    private readonly Mat _frame = new();

    private FrameSourceRuntime(FrameSourceConfig frameSourceConfig)
    {
        _frameSourceConfig = frameSourceConfig;
    }

    private void Start()
    {
        _frameSourceStream.SetFrameCallback(_frame, async ct =>
        {
            if (!IsRunning()) return;
            using var callbackLock = await _callbackLocker.WaitAsync(default);
            if (!IsRunning()) return;

            await FrameCallback(ct);
        });
    }

    private Task FrameCallback(CancellationToken cancellationToken)
    {
        var ss = _frame.ToBytes(ext: ".png");
        if (_frameSourceConfig.ShowWindow)
        {
            Cv2.ImShow($"Frame Server Source {_frameSourceConfig.Source}", _frame);
        }

        return Task.CompletedTask;
    }

    public async Task Open(CancellationToken cancellationToken)
    {
        using var _ = await _locker.WaitAsync(cancellationToken);
        await _frameSourceStream.Open(_frameSourceConfig, cancellationToken);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        using var _ = await _locker.WaitAsync(cancellationToken);
        await _frameSourceStream.Start(cancellationToken);
    }

    private bool IsRunning() =>
        _frameSourceConfig != null &&
        !IsDisposedOrDisposing &&
        !_frameSourceStream.VideoCapture.IsDisposed &&
        _frameSourceStream.VideoCapture.IsOpened();

    protected async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await _frameSourceStream.DisposeAsync();

            using var callbackLocker = await _callbackLocker.WaitAsync(default);
            using var locker = await _locker.WaitAsync(default);

            if (_frameSourceConfig.ShowWindow)
            {
                Cv2.DestroyWindow($"Frame Server Source {_frameSourceConfig.Source}");
            }

            InvokerUtils.RunAndForget(_frame.Release);
            InvokerUtils.RunAndForget(_frame.Dispose);
        }
    }
}
