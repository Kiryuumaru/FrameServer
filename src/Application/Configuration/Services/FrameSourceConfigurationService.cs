using Application.Common.Extensions;
using Application.Common.Features;
using DisposableHelpers;
using DisposableHelpers.Attributes;
using Domain.Events;
using Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Configuration.Services;

[Disposable]
internal partial class FrameSourceConfigurationService(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private readonly List<IDisposable> _disposables = [];

    private readonly Locker _locker = new();

    public Dictionary<string, FrameSourceConfig>? GetAllConfig()
    {
        var frameSourceConfigurationService = _serviceProvider.GetRequiredService<FrameSourceConfigurationHolderService>();
        return frameSourceConfigurationService.GetAllConfig();
    }

    public async Task<IDisposable> SubscribeFrameSourceAddedCallback(Action<FrameSourceAddedEventArgs> action, CancellationToken cancellationToken)
    {
        var frameSourceConfigurationService = _serviceProvider.GetRequiredService<FrameSourceConfigurationHolderService>();
        var disposable = await frameSourceConfigurationService.SubscribeFrameSourceAddedCallback(action, cancellationToken);
        using var _ = await _locker.WaitAsync(cancellationToken);
        _disposables.Add(disposable);
        return disposable;
    }

    public async Task<IDisposable> SubscribeFrameSourceModifiedCallback(Action<FrameSourceModifiedEventArgs> action, CancellationToken cancellationToken)
    {
        var frameSourceConfigurationService = _serviceProvider.GetRequiredService<FrameSourceConfigurationHolderService>();
        var disposable = await frameSourceConfigurationService.SubscribeFrameSourceModifiedCallback(action, cancellationToken);
        using var _ = await _locker.WaitAsync(cancellationToken);
        _disposables.Add(disposable);
        return disposable;
    }

    public async Task<IDisposable> SubscribeFrameSourceRemovedCallback(Action<FrameSourceRemovedEventArgs> action, CancellationToken cancellationToken)
    {
        var frameSourceConfigurationService = _serviceProvider.GetRequiredService<FrameSourceConfigurationHolderService>();
        var disposable = await frameSourceConfigurationService.SubscribeFrameSourceRemovedCallback(action, cancellationToken);
        using var _ = await _locker.WaitAsync(cancellationToken);
        _disposables.Add(disposable);
        return disposable;
    }

    protected async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            using var _ = await _locker.WaitAsync(default);
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
