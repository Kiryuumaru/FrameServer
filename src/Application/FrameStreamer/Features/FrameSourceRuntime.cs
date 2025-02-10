using Application.Common.Features;
using DisposableHelpers.Attributes;
using Domain.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.FrameStreamer.Features;

[Disposable]
public partial class FrameSourceRuntime(FrameSourceConfig frameSourceConfig)
{
    private readonly FrameSourceStream _frameSourceStream = new();

    public FrameSourceConfig FrameSourceConfig { get; private set; } = frameSourceConfig;

    public void SetFrameCallback(Mat frame, Func<CancellationToken, Task> callback)
    {
        _frameSourceStream.SetFrameCallback(frame, callback);
    }

    public async Task Open(CancellationToken cancellationToken)
    {
        await _frameSourceStream.Open(FrameSourceConfig, cancellationToken);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        await _frameSourceStream.Start(cancellationToken);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _frameSourceStream.Dispose();
        }
    }

    public async Task DisposeAndWaitStreamClose(CancellationToken cancellationToken)
    {
        Dispose();
        await _frameSourceStream.OpenGate.WaitForClosed(cancellationToken);
        await _frameSourceStream.StartGate.WaitForClosed(cancellationToken);
    }
}
