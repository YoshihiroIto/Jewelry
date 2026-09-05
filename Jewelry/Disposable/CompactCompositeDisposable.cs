using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Jewelry.Disposable;

public sealed class CompactCompositeDisposable : IDisposable
{
    private IDisposable? _first;
    private IDisposable? _second;
    private IDisposable? _third;
    private IDisposable? _fourth;
    private List<IDisposable>? _rest;
    private int _count;
    private bool _disposed;

    private Lock Gate
    {
        get
        {
            var gate = Volatile.Read(ref field);
            if (gate is { })
                return gate;

            var newGate = new Lock();
            var currentGate = Interlocked.CompareExchange(ref field, newGate, null);
            return currentGate ?? newGate;
        }
    }

    public CompactCompositeDisposable()
    {
    }

    public CompactCompositeDisposable(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);

        _first = disposable;
        _count = 1;
    }

    public CompactCompositeDisposable(IDisposable first, IDisposable second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        _first = first;
        _second = second;
        _count = 2;
    }

    public CompactCompositeDisposable(IDisposable first, IDisposable second, IDisposable third)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);

        _first = first;
        _second = second;
        _third = third;
        _count = 3;
    }

    public CompactCompositeDisposable(IDisposable first, IDisposable second, IDisposable third, IDisposable fourth)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(fourth);

        _first = first;
        _second = second;
        _third = third;
        _fourth = fourth;
        _count = 4;
    }

    public CompactCompositeDisposable(params IDisposable[] disposables)
        : this((IEnumerable<IDisposable>?)disposables ?? throw new ArgumentNullException(nameof(disposables)))
    {
    }

    public CompactCompositeDisposable(IEnumerable<IDisposable> disposables)
    {
        ArgumentNullException.ThrowIfNull(disposables);

        foreach (var disposable in disposables)
        {
            ArgumentNullException.ThrowIfNull(disposable, nameof(disposables));
            Add(disposable);
        }
    }

    public int Count
    {
        get
        {
            lock (Gate)
                return _count;
        }
    }

    public bool IsDisposed
    {
        get
        {
            lock (Gate)
                return _disposed;
        }
    }

    public void Add(IDisposable item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var disposeNow = false;

        lock (Gate)
        {
            if (_disposed)
            {
                disposeNow = true;
            }
            else
            {
                AddCore(item);
            }
        }

        if (disposeNow)
            item.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Action action)
    {
        Add(new AnonymousDisposable(action));
    }

    public void Clear()
    {
        IDisposable? first;
        IDisposable? second;
        IDisposable? third;
        IDisposable? fourth;
        List<IDisposable>? rest;

        lock (Gate)
        {
            if (_count is 0)
                return;

            first = _first;
            second = _second;
            third = _third;
            fourth = _fourth;
            rest = _rest;
            ClearStorageCore();
        }

        DisposeAll(first, second, third, fourth, rest);
    }

    public void Dispose()
    {
        IDisposable? first;
        IDisposable? second;
        IDisposable? third;
        IDisposable? fourth;
        List<IDisposable>? rest;

        lock (Gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            first = _first;
            second = _second;
            third = _third;
            fourth = _fourth;
            rest = _rest;
            ClearStorageCore();
        }

        DisposeAll(first, second, third, fourth, rest);
    }

    private void AddCore(IDisposable item)
    {
        switch (_count)
        {
            case 0:
                _first = item;
                break;
            case 1:
                _second = item;
                break;
            case 2:
                _third = item;
                break;
            case 3:
                _fourth = item;
                break;
            case 4:
                _rest = new List<IDisposable>(4) { item };
                break;
            default:
                _rest!.Add(item);
                break;
        }

        _count++;
    }

    private void ClearStorageCore()
    {
        _first = null;
        _second = null;
        _third = null;
        _fourth = null;
        _rest = null;
        _count = 0;
    }

    private static void DisposeAll(
        IDisposable? first,
        IDisposable? second,
        IDisposable? third,
        IDisposable? fourth,
        List<IDisposable>? rest)
    {
        first?.Dispose();
        second?.Dispose();
        third?.Dispose();
        fourth?.Dispose();

        if (rest is null)
            return;

        foreach (var disposable in rest)
            disposable.Dispose();
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