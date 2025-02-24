using DisposableHelpers;
using DisposableHelpers.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Features;

[Disposable]
public partial class Locker : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1);

    public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        return new Disposable(disposing =>
        {
            if (disposing)
            {
                _semaphore.Release();
            }
        });
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore?.Dispose();
        }
    }
}
