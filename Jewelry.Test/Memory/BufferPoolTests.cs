using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory;

public sealed class BufferPoolTests
{
    [Fact]
    public void Rent_ReturnsPooledBuffer_WhenSizeIsAtOrBelowThreshold()
    {
        // Arrange
        const int maxPooledBufferSize = 1024;
        const int requestSize = 512;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);

        // Act
        var buffer = bufferPool.Rent(requestSize);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= requestSize); // ArrayPool may return a larger array

        // Cleanup
        bufferPool.Return(buffer);
    }

    [Fact]
    public void Rent_AllocatesNewArray_WhenSizeExceedsThreshold()
    {
        // Arrange
        const int maxPooledBufferSize = 1024;
        const int requestSize = 2048;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);

        // Act
        var buffer = bufferPool.Rent(requestSize);

        // Assert
        Assert.NotNull(buffer);
        Assert.Equal(requestSize, buffer.Length);

        // Cleanup
        // This buffer was not from the pool, so Return should be a no-op.
        bufferPool.Return(buffer);
    }

    [Fact]
    public void Return_ReturnsBufferToPool_WhenSizeIsAtOrBelowThreshold()
    {
        // Arrange
        const int maxPooledBufferSize = 1024;
        const int size = 512;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);
        var buffer = bufferPool.Rent(size);

        // Act
        bufferPool.Return(buffer);

        // Assert (no exception means success)
    }

    [Fact]
    public void Return_DoesNotReturnBufferToPool_WhenSizeExceedsThreshold()
    {
        // Arrange
        const int maxPooledBufferSize = 1024;
        const int size = 2048;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);
        var buffer = bufferPool.Rent(size); // This allocates a new array

        // Act
        bufferPool.Return(buffer);

        // Assert (no exception means success)
    }

    [Fact]
    public void Rent_UsesThresholdAdjustedForUnsafeSizeOf()
    {
        // Arrange
        // sizeof(int) is 4. So maxPooledBufferSize in elements is 1024 / 4 = 256.
        const int maxPooledBufferSizeInBytes = 1024;
        const int requestSizeInElements = 256;
        var bufferPool = new BufferPool<int>(maxPooledBufferSizeInBytes);

        // Act
        // This should be rented from the pool.
        var bufferFromPool = bufferPool.Rent(requestSizeInElements);

        // This should be a new allocation.
        var newBuffer = bufferPool.Rent(requestSizeInElements + 1);

        // Assert
        Assert.True(bufferFromPool.Length >= requestSizeInElements);
        Assert.Equal(requestSizeInElements + 1, newBuffer.Length);

        // Cleanup
        bufferPool.Return(bufferFromPool);
        bufferPool.Return(newBuffer);
    }

    [Fact]
    public void RentAndReturn_DoNotThrow_ForZeroSize()
    {
        // Arrange
        const int maxPooledBufferSize = 1024;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);

        // Act
        var buffer = bufferPool.Rent(0);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= 0);

        // Cleanup
        bufferPool.Return(buffer);
    }

    [Fact]
    public void Rent_ThrowsArgumentOutOfRangeException_ForNegativeSize()
    {
        // Arrange
        const int maxPooledBufferSize = 1024;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => bufferPool.Rent(-1));
    }

    [Fact]
    public void Rent_ReturnsPooledBuffer_WhenSizeEqualsThreshold()
    {
        // Arrange
        const int maxPooledBufferSize = 1024; // byte要素のときは要素数の閾値も1024
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);

        // Act
        var buffer = bufferPool.Rent(1024);

        // Assert
        // プールからの貸出のため、長さは要求以上である可能性がある
        Assert.True(buffer.Length >= 1024);

        // Cleanup
        bufferPool.Return(buffer);
    }

    [Fact]
    public void Rent_AllocatesNewArray_WhenByteSizeExceedsThresholdByOne()
    {
        // Arrange
        const int maxPooledBufferSize = 1024;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);

        // Act
        var buffer = bufferPool.Rent(1025);

        // Assert
        // 新規割り当てのため長さはちょうど一致する
        Assert.Equal(1025, buffer.Length);

        // Cleanup
        bufferPool.Return(buffer);
    }

    [Fact]
    public void Rent_FloorsElementThreshold_ForNonDivisibleIntByteSize()
    {
        // Arrange
        const int maxPooledBufferSizeInBytes = 1025; // 1025 / 4 = 256 要素が閾値
        var bufferPool = new BufferPool<int>(maxPooledBufferSizeInBytes);

        // Act
        var fromPool = bufferPool.Rent(256); // 閾値ちょうど → プール
        var newAlloc = bufferPool.Rent(257); // 閾値超え → 新規割り当て

        // Assert
        Assert.True(fromPool.Length >= 256);
        Assert.Equal(257, newAlloc.Length);

        // Cleanup
        bufferPool.Return(fromPool);
        bufferPool.Return(newAlloc);
    }

    [Fact]
    public void Rent_DoesNotReuseArraysExceedingThreshold()
    {
        // Arrange
        const int maxPooledBufferSize = 256;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);
        var size = maxPooledBufferSize + 10; // 閾値超え

        // Act
        var buf1 = bufferPool.Rent(size);
        var buf2 = bufferPool.Rent(size);

        // Assert
        Assert.Equal(size, buf1.Length);
        Assert.Equal(size, buf2.Length);
        Assert.False(ReferenceEquals(buf1, buf2));

        // Cleanup
        bufferPool.Return(buf1);
        bufferPool.Return(buf2);
    }

    [Fact]
    public async Task RentAndReturn_DoNotThrow_WhenConcurrent()
    {
        // Arrange
        const int maxPooledBufferSize = 4096;
        var bufferPool = new BufferPool<byte>(maxPooledBufferSize);
        var tasks = new List<Task>();
        var rnd = new Random(123);

        // Act
        for (int i = 0; i < 64; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 256; j++)
                {
                    // 小サイズと大サイズを混在
                    var small = rnd.Next(0, 1024);
                    var large = rnd.Next(maxPooledBufferSize + 1, maxPooledBufferSize + 2048);

                    var b1 = bufferPool.Rent(small);
                    var b2 = bufferPool.Rent(large);

                    // 軽い利用: 先頭要素を書き込む（範囲内のみ）
                    if (b1.Length > 0) b1[0] = 1;
                    if (b2.Length > 0) b2[0] = 2;

                    bufferPool.Return(b1);
                    bufferPool.Return(b2);
                }
            }));
        }

        // Assert
        await Task.WhenAll(tasks);
    }
}
