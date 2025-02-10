using DisposableHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Features;

[Disposable]
public partial class AsyncAutoResetEvent1
{
    private readonly Lock _lock = new();
    private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public AsyncAutoResetEvent1(bool initialState = false)
    {
        if (initialState)
        {
            _tcs.TrySetResult(true);
        }

        AsyncAutoResetEvent ss = new();
    }

    public bool Wait(CancellationToken cancellationToken = default)
        => CoreWait(Timeout.InfiniteTimeSpan, cancellationToken);

    public bool Wait(TimeSpan timeout, CancellationToken cancellationToken = default)
        => CoreWait(timeout, cancellationToken);

    public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken = default)
        => CoreWait(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken);

    public ValueTask<bool> WaitAsync(CancellationToken cancellationToken = default)
        => CoreWaitAsync(Timeout.InfiniteTimeSpan, cancellationToken);

    public ValueTask<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        => CoreWaitAsync(timeout, cancellationToken);

    public ValueTask<bool> WaitAsync(int millisecondsTimeout, CancellationToken cancellationToken = default)
        => CoreWaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout), cancellationToken);

    private bool CoreWait(TimeSpan timeout, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        TaskCompletionSource<bool> tcs;
        lock (_lock)
        {
            if (_tcs.Task.IsCompleted)
            {
                return true;
            }
            tcs = _tcs;
        }

        try
        {
            var delayTask = Task.Delay(timeout, cancellationToken);
            var completedTask = Task.WhenAny(tcs.Task, delayTask).GetAwaiter().GetResult();
            return completedTask == tcs.Task;
        }
        catch { }

        return false;
    }

    private async ValueTask<bool> CoreWaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        TaskCompletionSource<bool> tcs;
        lock (_lock)
        {
            if (_tcs.Task.IsCompleted)
            {
                return true;
            }
            tcs = _tcs;
        }

        try
        {
            var delayTask = Task.Delay(timeout, cancellationToken);
            var completedTask = await Task.WhenAny(tcs.Task, delayTask).ConfigureAwait(false);
            return completedTask == tcs.Task;
        }
        catch { }

        return false;
    }

    public void Set()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        lock (_lock)
        {
            if (!_tcs.Task.IsCompleted)
            {
                _tcs.TrySetResult(true);
            }
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        lock (_lock)
        {
            if (_tcs.Task.IsCompleted)
            {
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            Set();
        }
    }
}
