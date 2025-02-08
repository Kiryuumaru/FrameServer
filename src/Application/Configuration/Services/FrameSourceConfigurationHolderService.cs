using Application.Common.Extensions;
using Application.Common.Features;
using DisposableHelpers;
using DisposableHelpers.Attributes;
using Domain.Events;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Configuration.Services;

internal partial class FrameSourceConfigurationHolderService
{
    private readonly Dictionary<string, FrameSourceConfig> _frameSources = [];

    private readonly Locker _locker = new();

    private readonly List<Action<FrameSourceAddedEventArgs>> _frameSourceAddedCallbacks = [];
    private readonly List<Action<FrameSourceModifiedEventArgs>> _frameSourceModifiedCallbacks = [];
    private readonly List<Action<FrameSourceRemovedEventArgs>> _frameSourceRemovedCallbacks = [];

    public Dictionary<string, FrameSourceConfig>? GetAllConfig()
    {
        return new Dictionary<string, FrameSourceConfig>(_frameSources);
    }

    private async Task<Disposable> SubscribeCallback(Action add, Action remove, CancellationToken cancellationToken)
    {
        using var _ = await _locker.WaitAsync(cancellationToken);
        add();
        return new Disposable(async disposing =>
        {
            using var _ = await _locker.WaitAsync(cancellationToken);
            remove();
        });
    }

    private async Task InvokeCallback<TCallbackArgs>(List<Action<TCallbackArgs>> callbacks, TCallbackArgs eventArgs, CancellationToken cancellationToken)
    {
        List<Action<TCallbackArgs>> callbackClone;
        {
            using var _ = await _locker.WaitAsync(cancellationToken);
            callbackClone = [.. callbacks];
        }
        foreach (var callback in callbackClone)
        {
            callback(eventArgs);
        }
    }

    public async Task<IDisposable> SubscribeFrameSourceAddedCallback(Action<FrameSourceAddedEventArgs> action, CancellationToken cancellationToken)
    {
        return await SubscribeCallback(() => _frameSourceAddedCallbacks.Add(action), () => _frameSourceAddedCallbacks.Remove(action), cancellationToken);
    }

    public async Task<IDisposable> SubscribeFrameSourceModifiedCallback(Action<FrameSourceModifiedEventArgs> action, CancellationToken cancellationToken)
    {
        return await SubscribeCallback(() => _frameSourceModifiedCallbacks.Add(action), () => _frameSourceModifiedCallbacks.Remove(action), cancellationToken);
    }

    public async Task<IDisposable> SubscribeFrameSourceRemovedCallback(Action<FrameSourceRemovedEventArgs> action, CancellationToken cancellationToken)
    {
        return await SubscribeCallback(() => _frameSourceRemovedCallbacks.Add(action), () => _frameSourceRemovedCallbacks.Remove(action), cancellationToken);
    }

    internal Task InvokeFrameSourceAddedCallback(FrameSourceAddedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        return InvokeCallback(_frameSourceAddedCallbacks, eventArgs, cancellationToken);
    }

    internal Task InvokeFrameSourceModifiedCallback(FrameSourceModifiedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        return InvokeCallback(_frameSourceModifiedCallbacks, eventArgs, cancellationToken);
    }

    internal Task InvokeFrameSourceRemovedCallback(FrameSourceRemovedEventArgs eventArgs, CancellationToken cancellationToken)
    {
        return InvokeCallback(_frameSourceRemovedCallbacks, eventArgs, cancellationToken);
    }
}
