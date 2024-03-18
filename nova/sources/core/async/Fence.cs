using System.Threading;
using System.Threading.Tasks;

namespace Nova;

public class Fence
{
    private TaskCompletionSource<object> _taskSource;
    /// <summary>
    /// The lock to ensure that all pauses are in sequence.
    /// </summary>
    private readonly SemaphoreSlim _pauseLock = new(1, 1);

    public bool Taken => _pauseLock.CurrentCount <= 0;

    public async Task<T> Take<T>(CancellationToken token)
    {
        await _pauseLock.WaitAsync(token);

        _taskSource = new TaskCompletionSource<object>();
        var registration = token.Register(() => _taskSource.SetCanceled());
        try
        {
            return (T)await _taskSource.Task;
        }
        finally
        {
            registration.Dispose();
            _taskSource = null;
            _pauseLock.Release();
        }
    }

    public async Task Take(CancellationToken token)
    {
        await Take<object>(token);
    }

    public void Signal<T>(T result)
    {
        _taskSource?.SetResult(result);
    }

    public void Signal()
    {
        _taskSource?.SetResult(null);
    }

    /// <summary>
    /// Take the fence and immediately release it.
    /// This ensures all existing fences are finished.
    /// </summary>
    public async Task Barrier(CancellationToken token)
    {
        await _pauseLock.WaitAsync(token);
        // no need to actually take fence and signal it.
        _pauseLock.Release();
    }

    // TODO: do we need reset?
}
