using DisposableHelpers;
using Domain.Events;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Configuration.Services;

internal class FrameSourceConfigurationService
{
    private readonly Dictionary<string, FrameSourceConfig> _frameSources = [];

    private readonly SemaphoreSlim _locker = new(1);

    private readonly List<Action<FrameSourceAddedEventArgs>> _frameSourceAddedCallbacks = [];
    private readonly List<Action<FrameSourceModifiedEventArgs>> _frameSourceModifiedCallbacks = [];
    private readonly List<Action<FrameSourceRemovedEventArgs>> _frameSourceRemovedCallbacks = [];

    public Dictionary<string, FrameSourceConfig>? GetAllConfig()
    {
        return new Dictionary<string, FrameSourceConfig>(_frameSources);
    }

    private Disposable SubscribeCallback(Action add, Action remove)
    {
        _locker.Wait();
        try
        {
            add();
            return new Disposable(disposing =>
            {
                _locker.Wait();
                try
                {
                    remove();
                }
                finally
                {
                    _locker.Release();
                }
            });
        }
        finally
        {
            _locker.Release();
        }
    }

    private void InvokeCallback<TCallbackArgs>(List<Action<TCallbackArgs>> callbacks, TCallbackArgs eventArgs)
    {
        List<Action<TCallbackArgs>> callbackClone;
        _locker.Wait();
        try
        {
            callbackClone = [.. callbacks];
        }
        finally
        {
            _locker.Release();
        }
        foreach (var callback in callbackClone)
        {
            callback(eventArgs);
        }
    }

    public IDisposable SubscribeFrameSourceAddedCallback(Action<FrameSourceAddedEventArgs> action)
    {
        return SubscribeCallback(() => _frameSourceAddedCallbacks.Add(action), () => _frameSourceAddedCallbacks.Remove(action));
    }

    public IDisposable SubscribeFrameSourceModifiedCallback(Action<FrameSourceModifiedEventArgs> action)
    {
        return SubscribeCallback(() => _frameSourceModifiedCallbacks.Add(action), () => _frameSourceModifiedCallbacks.Remove(action));
    }

    public IDisposable SubscribeFrameSourceRemovedCallback(Action<FrameSourceRemovedEventArgs> action)
    {
        return SubscribeCallback(() => _frameSourceRemovedCallbacks.Add(action), () => _frameSourceRemovedCallbacks.Remove(action));
    }

    internal void InvokeFrameSourceAddedCallback(FrameSourceAddedEventArgs eventArgs)
    {
        InvokeCallback(_frameSourceAddedCallbacks, eventArgs);
    }

    internal void InvokeFrameSourceModifiedCallback(FrameSourceModifiedEventArgs eventArgs)
    {
        InvokeCallback(_frameSourceModifiedCallbacks, eventArgs);
    }

    internal void InvokeFrameSourceRemovedCallback(FrameSourceRemovedEventArgs eventArgs)
    {
        InvokeCallback(_frameSourceRemovedCallbacks, eventArgs);
    }
}
