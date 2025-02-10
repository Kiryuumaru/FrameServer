using Application.Common.Extensions;

namespace Application.Common.Features;

public class GateKeeper
{
    private readonly AsyncManualResetEvent _openWaiterEvent = new(false);
    private readonly AsyncManualResetEvent _closedWaiterEvent = new(true);
    private readonly Lock _locker = new();

    public bool IsOpen { get; private set; }

    public bool IsClosed { get; private set; }

    public GateKeeper(bool initialOpenState = false)
    {
        SetOpen(initialOpenState);
    }

    public void SetOpen(bool isOpen = true)
    {
        lock (_locker)
        {
            IsOpen = isOpen;
            IsClosed = !isOpen;

            if (isOpen)
            {
                _openWaiterEvent.Set();
                _closedWaiterEvent.Reset();
            }
            else
            {
                _openWaiterEvent.Reset();
                _closedWaiterEvent.Set();
            }
        }
    }

    public void SetClosed(bool isClosed = true)
    {
        SetOpen(!isClosed);
    }

    public ValueTask<bool> WaitForOpen(CancellationToken cancellationToken)
        => _openWaiterEvent.WaitAsync(cancellationToken);

    public ValueTask<bool> WaitForClosed(CancellationToken cancellationToken)
        => _closedWaiterEvent.WaitAsync(cancellationToken);
}
