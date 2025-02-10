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

    private readonly List<Func<FrameSourceAddedEventArgs, CancellationToken, Task>> _frameSourceAddedCallbacks = [];
    private readonly List<Func<FrameSourceModifiedEventArgs, CancellationToken, Task>> _frameSourceModifiedCallbacks = [];
    private readonly List<Func<FrameSourceRemovedEventArgs, CancellationToken, Task>> _frameSourceRemovedCallbacks = [];

    public Dictionary<string, FrameSourceConfig> GetAllConfig()
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

    private async Task InvokeCallback<TCallbackArgs>(List<Func<TCallbackArgs, CancellationToken, Task>> callbacks, TCallbackArgs eventArgs, CancellationToken cancellationToken)
    {
        using var _ = await _locker.WaitAsync(cancellationToken);

        List<Task> tasks = [];
        foreach (var callback in callbacks)
        {
            tasks.Add(callback(eventArgs, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }

    public async Task<IDisposable> SubscribeFrameSourceAddedCallback(Func<FrameSourceAddedEventArgs, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        return await SubscribeCallback(() => _frameSourceAddedCallbacks.Add(action), () => _frameSourceAddedCallbacks.Remove(action), cancellationToken);
    }

    public async Task<IDisposable> SubscribeFrameSourceModifiedCallback(Func<FrameSourceModifiedEventArgs, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        return await SubscribeCallback(() => _frameSourceModifiedCallbacks.Add(action), () => _frameSourceModifiedCallbacks.Remove(action), cancellationToken);
    }

    public async Task<IDisposable> SubscribeFrameSourceRemovedCallback(Func<FrameSourceRemovedEventArgs, CancellationToken, Task> action, CancellationToken cancellationToken)
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
