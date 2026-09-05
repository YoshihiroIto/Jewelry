using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Jewelry.Disposable;

public static class CompactCompositeDisposableExtensions
{
    extension<T>(T disposable)
        where T : IDisposable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T AddTo(CompactCompositeDisposable compositeDisposable)
        {
            compositeDisposable.Add(disposable);
            return disposable;
        }
    }

    extension(CompactCompositeDisposable c)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Action action)
        {
            c.Add(new AnonymousDisposable(action));
        }
    }
}

file sealed class AnonymousDisposable(Action dispose) : IDisposable
{
    public bool IsDisposed => _dispose == null;

    private volatile Action? _dispose = dispose;

    public void Dispose()
    {
        var action = Interlocked.Exchange<Action>(ref _dispose!, null!);

        action?.Invoke();
    }
}