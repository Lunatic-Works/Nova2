using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nova;

public class Event
{
    private Action _event;

    public void Subscribe(Action callback)
    {
        _event += callback;
    }

    public void Unsubscribe(Action callback)
    {
        _event -= callback;
    }

    public void Invoke()
    {
        _event?.Invoke();
    }

    public async Task Wait()
    {
        var taskSource = new TaskCompletionSource();
        void CompleteTask() => taskSource.SetResult();
        _event += CompleteTask;
        try
        {
            await taskSource.Task;
        }
        finally
        {
            _event -= CompleteTask;
        }
    }

    public async Task Wait(CancellationToken token)
    {
        var taskSource = new TaskCompletionSource();
        void CompleteTask() => taskSource.SetResult();
        void CancelTask()
        {
            _event -= CompleteTask;
            taskSource.SetCanceled(token);
        }
        _event += CompleteTask;
        var registration = token.Register(CancelTask);
        try
        {
            await taskSource.Task;
        }
        finally
        {
            _event -= CompleteTask;
            registration.Dispose();
        }
    }
}

public class Event<T>
{
    private event Action<T> _event;

    public void Subscribe(Action<T> callback)
    {
        _event += callback;
    }

    public void Unsubscribe(Action<T> callback)
    {
        _event -= callback;
    }

    public void Invoke(T arg)
    {
        _event?.Invoke(arg);
    }

    public async Task<T> Wait()
    {
        var taskSource = new TaskCompletionSource<T>();
        void CompleteTask(T result) => taskSource.SetResult(result);
        _event += CompleteTask;
        try
        {
            return await taskSource.Task;
        }
        finally
        {
            _event -= CompleteTask;
        }
    }

    public async Task<T> Wait(CancellationToken token)
    {
        var taskSource = new TaskCompletionSource<T>();
        void CompleteTask(T result) => taskSource.SetResult(result);
        void CancelTask()
        {
            _event -= CompleteTask;
            taskSource.SetCanceled(token);
        }
        _event += CompleteTask;
        var registration = token.Register(CancelTask);
        try
        {
            return await taskSource.Task;
        }
        finally
        {
            _event -= CompleteTask;
            registration.Dispose();
        }
    }
}
