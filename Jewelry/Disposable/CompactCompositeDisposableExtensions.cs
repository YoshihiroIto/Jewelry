using System;
using System.Runtime.CompilerServices;

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
}