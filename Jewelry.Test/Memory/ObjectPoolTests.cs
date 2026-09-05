using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory;

public sealed class ObjectPoolTests
{
    private const int ItemPoolCount = 16;

    private class PooledObject;

    private sealed class AllocationCounterScope : IDisposable
    {
        private static readonly AsyncLocal<AllocationCounterScope?> Current = new();
        private readonly AllocationCounterScope? _previous;

        private AllocationCounterScope()
        {
            _previous = Current.Value;
            Current.Value = this;
        }

        public int AllocationCount;

        public static AllocationCounterScope Enter() => new();

        public static void Increment()
        {
            var current = Current.Value ?? throw new InvalidOperationException("Allocation counter scope is not set.");
            Interlocked.Increment(ref current.AllocationCount);
        }

        public void Dispose()
        {
            Current.Value = _previous;
        }
    }

    private sealed class CountedPooledObject
    {
        public CountedPooledObject()
        {
            AllocationCounterScope.Increment();
        }

        public static readonly object Gate = new();
    }

    [Fact]
    public void Get_ReturnsNewInstance_WhenPoolIsEmpty()
    {
        // Arrange
        var pool = new ObjectPool<PooledObject>(ItemPoolCount);

        // Act
        var obj = pool.Get();

        // Assert
        Assert.NotNull(obj);
    }

    [Fact]
    public void Get_ReturnsExistingInstance_WhenPoolContainsObject()
    {
        // Arrange
        var pool = new ObjectPool<PooledObject>(ItemPoolCount);
        var initialObj = pool.Get();
        pool.Return(initialObj);

        // Act
        var retrievedObj = pool.Get();

        // Assert
        Assert.Same(initialObj, retrievedObj);
    }

    [Fact]
    public void Return_AddsObjectToPool()
    {
        // Arrange
        var pool = new ObjectPool<PooledObject>(ItemPoolCount);
        var objToReturn = new PooledObject();

        // Act
        pool.Return(objToReturn);
        var retrievedObj = pool.Get();

        // Assert
        Assert.Same(objToReturn, retrievedObj);
    }

    [Fact]
    public void Get_ReturnsDifferentInstances_WhenCalledMultipleTimes()
    {
        // Arrange
        var pool = new ObjectPool<PooledObject>(ItemPoolCount);

        // Act
        var obj1 = pool.Get();
        var obj2 = pool.Get();

        // Assert
        Assert.NotSame(obj1, obj2);
    }

    [Fact]
    public void Get_ReusesInstance_AfterReturn()
    {
        // Arrange
        var pool = new ObjectPool<PooledObject>(ItemPoolCount);
        var obj1 = pool.Get();

        // Act
        pool.Return(obj1);
        var obj2 = pool.Get();

        // Assert
        Assert.Same(obj1, obj2);
    }

    [Fact]
    public void Get_DoesNotAllocate_WhenRequestsDoNotExceedReturnedCount()
    {
        // Arrange
        using var counter = AllocationCounterScope.Enter();
        var pool = new ObjectPool<CountedPooledObject>(ItemPoolCount);
        var prepared = new List<CountedPooledObject>();

        // 5個を新規作成してプールへ返す
        for (int i = 0; i < 5; i++)
        {
            var obj = pool.Get();
            prepared.Add(obj);
        }

        foreach (var o in prepared)
            pool.Return(o);

        lock (CountedPooledObject.Gate)
        {
            var allocatedBefore = counter.AllocationCount;

            // Act
            var acquired = new List<CountedPooledObject>();
            for (int i = 0; i < 5; i++)
                acquired.Add(pool.Get());

            // Assert
            Assert.Equal(allocatedBefore, counter.AllocationCount);

            // 後片付け
            foreach (var o in acquired)
                pool.Return(o);
        }
    }

    [Fact]
    public void Get_AllocatesOnlyExcessInstances_WhenRequestsExceedReturnedCount()
    {
        // Arrange
        using var counter = AllocationCounterScope.Enter();
        var pool = new ObjectPool<CountedPooledObject>(ItemPoolCount);

        // 3つプールしておく
        var tmp = new List<CountedPooledObject>();
        for (int i = 0; i < 3; i++) tmp.Add(pool.Get());
        foreach (var o in tmp) pool.Return(o);

        lock (CountedPooledObject.Gate)
        {
            var before = counter.AllocationCount; // = 3

            // Act: 5個要求（超過2）
            var list = new List<CountedPooledObject>();
            for (int i = 0; i < 5; i++) list.Add(pool.Get());

            // Assert
            Assert.Equal(before + 2, counter.AllocationCount);

            // 後片付け
            foreach (var o in list) pool.Return(o);
        }
    }

    [Fact]
    public async Task Get_ReturnsDistinctInstances_WhenCalledConcurrently()
    {
        // Arrange
        var pool = new ObjectPool<PooledObject>(ItemPoolCount);
        var count = 100;

        // Act
        var tasks = Enumerable.Range(0, count)
            .Select(_ => Task.Run(() => pool.Get()))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(count, results.Length);
        Assert.Equal(count, results.Distinct().Count());
    }

    [Fact]
    public async Task AllocationCounts_AreIsolatedPerTest_WhenRunConcurrently()
    {
        // Arrange
        static int RunIsolatedAllocationCount(int count)
        {
            using var counter = AllocationCounterScope.Enter();
            var pool = new ObjectPool<CountedPooledObject>(ItemPoolCount);

            for (int i = 0; i < count; i++)
                _ = pool.Get();

            return counter.AllocationCount;
        }

        // Act
        var counts = await Task.WhenAll(
            Task.Run(() => RunIsolatedAllocationCount(3)),
            Task.Run(() => RunIsolatedAllocationCount(5)));

        // Assert
        Assert.Equal(3, counts[0]);
        Assert.Equal(5, counts[1]);
    }
}
