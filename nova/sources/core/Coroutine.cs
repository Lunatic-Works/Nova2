using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nova;

public class Coroutine
{
    public readonly Task Task;
    private readonly CancellationTokenSource _cancellationSource;

    private Coroutine(Func<CancellationToken, Task> asyncFunc)
    {
        _cancellationSource = new();
        Task = asyncFunc.Invoke(_cancellationSource.Token);
    }

    public void Cancel()
    {
        _cancellationSource.Cancel();
        _cancellationSource.Dispose();
    }

    public static Coroutine Start(Func<CancellationToken, Task> asyncFunc)
    {
        return new Coroutine(asyncFunc);
    }
}
