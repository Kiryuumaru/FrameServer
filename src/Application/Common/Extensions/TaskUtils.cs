namespace Application.Common.Extensions;

public static class TaskUtils
{
    public static async Task DelayAndForget(int milliseconds, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(milliseconds, cancellationToken);
        }
        catch { }
    }

    public static async Task DelayAndForget(TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(timeSpan, cancellationToken);
        }
        catch { }
    }

    public static void Forget(this Task task)
    {
        if (!task.IsCompleted || task.IsFaulted)
        {
            _ = ForgetAwaited(task);
        }

        async static Task ForgetAwaited(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch { }
        }
    }

    public static Task WaitThread(this Task task)
    {
        return ThreadHelpers.WaitThread(() => task);
    }

    public static async Task RunWithCancellationToken(Func<CancellationToken, Task> action, CancellationToken externalToken)
    {
        var task = action(externalToken);

        if (await Task.WhenAny(task, Task.Delay(Timeout.Infinite, externalToken)) == task)
        {
            await task;
        }
        else
        {
            throw new OperationCanceledException(externalToken);
        }
    }
}
