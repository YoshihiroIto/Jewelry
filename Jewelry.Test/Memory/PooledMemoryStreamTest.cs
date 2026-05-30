using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory;

public class PooledMemoryStreamTest
{
    // ════════════════════════════════════════════════════════════════════════
    // 基本的な構築とプロパティ
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void DefaultConstructor_InitialState()
    {
        using var ms = new PooledMemoryStream();

        Assert.Equal(0L, ms.Length);
        Assert.Equal(0L, ms.Position);
        Assert.True(ms.CanRead);
        Assert.True(ms.CanSeek);
        Assert.True(ms.CanWrite);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(4096)]
    public void Constructor_WithCapacity_InitialState(int capacity)
    {
        using var ms = new PooledMemoryStream(capacity);

        Assert.Equal(0L, ms.Length);
        Assert.Equal(0L, ms.Position);
        Assert.True(ms.Capacity >= 0);
    }

    [Fact]
    public void Constructor_NegativeCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PooledMemoryStream(-1));
    }

    [Fact]
    public void Constructor_NullPool_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PooledMemoryStream(256, null!));
    }

    [Fact]
    public void Constructor_WithCustomPool()
    {
        var pool = ArrayPool<byte>.Create(1024, 10);
        using var ms = new PooledMemoryStream(256, pool);

        ms.WriteByte(42);
        Assert.Equal(1, ms.Length);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Constructor with initial buffer (byte[] / ReadOnlySpan)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Constructor_ByteArray_CopiesData()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var ms = new PooledMemoryStream(data);

        Assert.Equal(data.Length, ms.Length);
        Assert.Equal(0L, ms.Position);
        Assert.Equal(data, ms.ToArray());
    }

    [Fact]
    public void Constructor_ByteArray_Empty()
    {
        using var ms = new PooledMemoryStream(Array.Empty<byte>());

        Assert.Equal(0L, ms.Length);
        Assert.Equal(0L, ms.Position);
    }

    [Fact]
    public void Constructor_ByteArray_IsIndependentCopy()
    {
        var data = new byte[] { 1, 2, 3 };
        using var ms = new PooledMemoryStream(data);

        // 元配列を変更してもストリームの内容は変わらない
        data[0] = 99;
        Assert.Equal(1, ms.ToArray()[0]);
    }

    [Fact]
    public void Constructor_ByteArray_WithCustomPool()
    {
        var pool = ArrayPool<byte>.Create(1024, 10);
        var data = new byte[] { 10, 20, 30 };
        using var ms = new PooledMemoryStream(data, pool);

        Assert.Equal(data, ms.ToArray());
    }

    [Fact]
    public void Constructor_ByteArray_CanReadAfterConstruction()
    {
        var data = new byte[] { 7, 8, 9 };
        using var ms = new PooledMemoryStream(data);

        Assert.Equal(7, ms.ReadByte());
        Assert.Equal(8, ms.ReadByte());
        Assert.Equal(9, ms.ReadByte());
        Assert.Equal(-1, ms.ReadByte()); // EOF
    }

    [Fact]
    public void Constructor_ByteArray_CanWriteAfterConstruction()
    {
        var data = new byte[] { 1, 2, 3 };
        using var ms = new PooledMemoryStream(data);

        // 末尾に追記
        ms.Position = ms.Length;
        ms.WriteByte(4);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, ms.ToArray());
    }

    [Fact]
    public void Constructor_ByteArray_CanOverwriteInPlace()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var ms = new PooledMemoryStream(data);

        ms.Position = 1;
        ms.Write(new byte[] { 20, 30 }, 0, 2);

        Assert.Equal(new byte[] { 1, 20, 30, 4, 5 }, ms.ToArray());
    }

    [Fact]
    public void Constructor_Span_CopiesData()
    {
        ReadOnlySpan<byte> span = new byte[] { 10, 20, 30 };
        using var ms = new PooledMemoryStream(span);

        Assert.Equal(3L, ms.Length);
        Assert.Equal(0L, ms.Position);
        Assert.Equal(new byte[] { 10, 20, 30 }, ms.ToArray());
    }

    [Fact]
    public void Constructor_Span_Empty()
    {
        using var ms = new PooledMemoryStream(ReadOnlySpan<byte>.Empty);

        Assert.Equal(0L, ms.Length);
        Assert.Equal(0L, ms.Position);
    }

    [Fact]
    public void Constructor_Span_WithCustomPool()
    {
        var pool = ArrayPool<byte>.Create(1024, 10);
        ReadOnlySpan<byte> span = new byte[] { 5, 10, 15 };
        using var ms = new PooledMemoryStream(span, pool);

        Assert.Equal(new byte[] { 5, 10, 15 }, ms.ToArray());
    }

    // ════════════════════════════════════════════════════════════════════════
    // WriteByte / ReadByte
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void WriteByte_Then_ReadByte()
    {
        using var ms = new PooledMemoryStream();

        ms.WriteByte(0xAB);
        ms.WriteByte(0xCD);

        Assert.Equal(2L, ms.Length);
        Assert.Equal(2L, ms.Position);

        ms.Position = 0;
        Assert.Equal(0xAB, ms.ReadByte());
        Assert.Equal(0xCD, ms.ReadByte());
        Assert.Equal(-1, ms.ReadByte()); // EOF
    }

    [Fact]
    public void ReadByte_AtEnd_ReturnsMinusOne()
    {
        using var ms = new PooledMemoryStream();
        Assert.Equal(-1, ms.ReadByte());
    }

    // ════════════════════════════════════════════════════════════════════════
    // Write / Read (byte 配列)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Write_Read_RoundTrip()
    {
        using var ms = new PooledMemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };

        ms.Write(data, 0, data.Length);
        Assert.Equal(5L, ms.Length);

        ms.Position = 0;
        var result = new byte[data.Length];
        var read = ms.Read(result, 0, result.Length);

        Assert.Equal(data.Length, read);
        Assert.Equal(data, result);
    }

    [Fact]
    public void Write_WithOffset_CorrectData()
    {
        using var ms = new PooledMemoryStream();
        var data = new byte[] { 0, 1, 2, 3, 4 };

        ms.Write(data, 2, 3); // 2, 3, 4 を書き込む

        ms.Position = 0;
        var result = new byte[3];
        ms.ReadExactly(result, 0, result.Length);

        Assert.Equal(new byte[] { 2, 3, 4 }, result);
    }

    [Fact]
    public void Read_ReturnsPartialWhenNearEnd()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        ms.Position = 2;
        var result = new byte[10]; // データより大きいバッファ
        var read = ms.Read(result, 0, result.Length);

        Assert.Equal(1, read);
        Assert.Equal(3, result[0]); // position=2 にある値（3番目の要素）
        Assert.Equal(0, result[1]); // 未書き込みなので 0 のまま
    }

    [Fact]
    public void Read_ZeroCount_ReturnsZero()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2 }, 0, 2);
        ms.Position = 0;

        var result = new byte[10];
        var read = ms.Read(result, 0, 0);
        Assert.Equal(0, read);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Write / Read (Span)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void WriteSpan_ReadSpan_RoundTrip()
    {
        using var ms = new PooledMemoryStream();
        var data = new byte[] { 10, 20, 30, 40 };

        ms.Write(data.AsSpan());

        ms.Position = 0;
        var result = new byte[4];
        var read = ms.Read(result.AsSpan());

        Assert.Equal(4, read);
        Assert.Equal(data, result);
    }

    [Fact]
    public void WriteSpan_Empty_NoEffect()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(ReadOnlySpan<byte>.Empty);
        Assert.Equal(0L, ms.Length);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Seek
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Seek_Begin()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        ms.Seek(2, SeekOrigin.Begin);
        Assert.Equal(2L, ms.Position);
    }

    [Fact]
    public void Seek_Current()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        ms.Seek(1, SeekOrigin.Begin);
        ms.Seek(2, SeekOrigin.Current);
        Assert.Equal(3L, ms.Position);
    }

    [Fact]
    public void Seek_End()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        ms.Seek(-2, SeekOrigin.End);
        Assert.Equal(3L, ms.Position);
    }

    [Fact]
    public void Seek_BeforeBegin_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<IOException>(() => ms.Seek(-1, SeekOrigin.Begin));
    }

    [Fact]
    public void Seek_BeyondEnd_AllowedPositionDoesNotExtendLength()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        // MemoryStream と同様に、Seek でストリーム末尾を超えた位置へ移動できる
        ms.Seek(100, SeekOrigin.Begin);
        Assert.Equal(100L, ms.Position);
        Assert.Equal(3L, ms.Length); // Length は変わらない
    }

    [Fact]
    public void Seek_InvalidOrigin_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentException>(() => ms.Seek(0, (SeekOrigin)99));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Position プロパティ
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Position_Set_Negative_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ms.Position = -1);
    }

    [Fact]
    public void Position_Set_BeyondEnd_Valid()
    {
        using var ms = new PooledMemoryStream();
        ms.Position = 1000;
        Assert.Equal(1000L, ms.Position);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SetLength
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SetLength_Expand_ZeroPadsNewArea()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        ms.SetLength(6);
        Assert.Equal(6L, ms.Length);

        ms.Position = 3;
        Assert.Equal(0, ms.ReadByte());
        Assert.Equal(0, ms.ReadByte());
        Assert.Equal(0, ms.ReadByte());
    }

    [Fact]
    public void SetLength_Shrink_TruncatesData()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
        ms.Position = 4;

        ms.SetLength(2);
        Assert.Equal(2L, ms.Length);
        Assert.Equal(2L, ms.Position); // Position はクランプされる
    }

    [Fact]
    public void SetLength_Negative_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ms.SetLength(-1));
    }

    [Fact]
    public void SetLength_Zero_EmptiesStream()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        ms.SetLength(0);
        Assert.Equal(0L, ms.Length);
        Assert.Equal(0L, ms.Position);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Capacity
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Capacity_SetLarger_Retained()
    {
        using var ms = new PooledMemoryStream(8);
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        ms.Capacity = 512;
        Assert.True(ms.Capacity >= 512);

        // データは保持される
        ms.Position = 0;
        Assert.Equal(1, ms.ReadByte());
        Assert.Equal(2, ms.ReadByte());
        Assert.Equal(3, ms.ReadByte());
    }

    [Fact]
    public void Capacity_SetSmallerThanLength_Throws()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => ms.Capacity = 2);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ToArray
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToArray_ReturnsExactCopy()
    {
        using var ms = new PooledMemoryStream();
        var data = new byte[] { 10, 20, 30 };
        ms.Write(data, 0, data.Length);

        var result = ms.ToArray();

        Assert.Equal(data, result);
        Assert.NotSame(data, result); // コピーであること
    }

    [Fact]
    public void ToArray_Empty_ReturnsEmptyArray()
    {
        using var ms = new PooledMemoryStream();
        var result = ms.ToArray();
        Assert.Empty(result);
    }

    [Fact]
    public void ToArray_IsIndependentOfSubsequentWrites()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        var snapshot = ms.ToArray();

        ms.Write(new byte[] { 4, 5, 6 }, 0, 3);

        // スナップショットは変更されない
        Assert.Equal(new byte[] { 1, 2, 3 }, snapshot);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GetBuffer / GetBufferSpan
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetBuffer_ReturnsDirectReference()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        var buffer = ms.GetBuffer();

        Assert.Equal(3, buffer.Length);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer.ToArray());
    }

    [Fact]
    public void GetBufferSpan_ReturnsCorrectData()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 5, 10, 15 }, 0, 3);

        var span = ms.GetBufferSpan();

        Assert.Equal(3, span.Length);
        Assert.Equal(5, span[0]);
        Assert.Equal(10, span[1]);
        Assert.Equal(15, span[2]);
    }

    // ════════════════════════════════════════════════════════════════════════
    // WriteTo
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void WriteTo_CopiesAllContent()
    {
        using var source = new PooledMemoryStream();
        var data = new byte[] { 1, 2, 3, 4, 5 };
        source.Write(data, 0, data.Length);

        using var dest = new MemoryStream();
        source.WriteTo(dest);

        Assert.Equal(data, dest.ToArray());
    }

    [Fact]
    public void WriteTo_NullDestination_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentNullException>(() => ms.WriteTo(null!));
    }

    [Fact]
    public void WriteTo_Empty_WritesNothing()
    {
        using var source = new PooledMemoryStream();
        using var dest = new MemoryStream();
        source.WriteTo(dest);

        Assert.Equal(0L, dest.Length);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 自動拡張
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AutoGrow_SmallInitialCapacity()
    {
        using var ms = new PooledMemoryStream(4);

        // 初期容量をはるかに超えるデータを書き込む
        var data = Enumerable.Range(0, 1024).Select(i => (byte)(i % 256)).ToArray();
        ms.Write(data, 0, data.Length);

        Assert.Equal(1024L, ms.Length);

        ms.Position = 0;
        var result = ms.ToArray();
        Assert.Equal(data, result);
    }

    [Fact]
    public void AutoGrow_WriteByteRepeated()
    {
        using var ms = new PooledMemoryStream(0);

        for (var i = 0; i < 500; i++)
            ms.WriteByte((byte)(i % 256));

        Assert.Equal(500L, ms.Length);

        ms.Position = 0;
        for (var i = 0; i < 500; i++)
            Assert.Equal(i % 256, ms.ReadByte());
    }

    // ════════════════════════════════════════════════════════════════════════
    // 上書き書き込み（中間位置への書き込み）
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Write_OverwriteInMiddle()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        ms.Position = 1;
        ms.Write(new byte[] { 20, 30 }, 0, 2);

        Assert.Equal(5L, ms.Length); // 長さは変わらない
        Assert.Equal(new byte[] { 1, 20, 30, 4, 5 }, ms.ToArray());
    }

    [Fact]
    public void Write_OverlapEnd_ExtendsLength()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);

        ms.Position = 2;
        ms.Write(new byte[] { 10, 20, 30 }, 0, 3);

        Assert.Equal(5L, ms.Length);
        Assert.Equal(new byte[] { 1, 2, 10, 20, 30 }, ms.ToArray());
    }

    // ════════════════════════════════════════════════════════════════════════
    // Dispose 後のアクセス
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void AfterDispose_CanRead_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = ms.CanRead);
    }

    [Fact]
    public void AfterDispose_CanSeek_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = ms.CanSeek);
    }

    [Fact]
    public void AfterDispose_CanWrite_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = ms.CanWrite);
    }

    [Fact]
    public void AfterDispose_Length_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = ms.Length);
    }

    [Fact]
    public void AfterDispose_Position_Get_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => _ = ms.Position);
    }

    [Fact]
    public void AfterDispose_Position_Set_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.Position = 0);
    }

    [Fact]
    public void AfterDispose_Write_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.Write(new byte[] { 1 }, 0, 1));
    }

    [Fact]
    public void AfterDispose_WriteByte_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.WriteByte(1));
    }

    [Fact]
    public void AfterDispose_Read_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.Read(new byte[1], 0, 1));
    }

    [Fact]
    public void AfterDispose_ReadByte_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.ReadByte());
    }

    [Fact]
    public void AfterDispose_Seek_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.Seek(0, SeekOrigin.Begin));
    }

    [Fact]
    public void AfterDispose_SetLength_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.SetLength(0));
    }

    [Fact]
    public void AfterDispose_ToArray_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.ToArray());
    }

    [Fact]
    public void AfterDispose_GetBuffer_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ms.GetBuffer());
    }

    [Fact]
    public void AfterDispose_WriteTo_Throws()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        using var dest = new MemoryStream();
        Assert.Throws<ObjectDisposedException>(() => ms.WriteTo(dest));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_NothingThrows()
    {
        var ms = new PooledMemoryStream();
        ms.Dispose();
        ms.Dispose(); // 2回呼んでも例外なし
    }

    // ════════════════════════════════════════════════════════════════════════
    // 引数検証
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Write_NullBuffer_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentNullException>(() => ms.Write(null!, 0, 0));
    }

    [Fact]
    public void Write_NegativeOffset_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ms.Write(new byte[4], -1, 1));
    }

    [Fact]
    public void Write_NegativeCount_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ms.Write(new byte[4], 0, -1));
    }

    [Fact]
    public void Write_CountExceedsBuffer_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ms.Write(new byte[4], 0, 5));
    }

    [Fact]
    public void Read_NullBuffer_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentNullException>(() => ms.Read(null!, 0, 0));
    }

    [Fact]
    public void Read_NegativeOffset_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ms.Read(new byte[4], -1, 1));
    }

    [Fact]
    public void Read_CountExceedsBuffer_Throws()
    {
        using var ms = new PooledMemoryStream();
        Assert.Throws<ArgumentOutOfRangeException>(() => ms.Read(new byte[4], 0, 5));
    }

    // ════════════════════════════════════════════════════════════════════════
    // MemoryStream との等価性検証
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 42 })]
    [InlineData(new byte[] { 1, 2, 3, 4, 5 })]
    public void BehaviorMatchesMemoryStream_WriteAndRead(byte[] data)
    {
        using var pooled = new PooledMemoryStream();
        using var standard = new MemoryStream();

        pooled.Write(data, 0, data.Length);
        standard.Write(data, 0, data.Length);

        Assert.Equal(standard.Length, pooled.Length);
        Assert.Equal(standard.Position, pooled.Position);

        pooled.Position = 0;
        standard.Position = 0;

        Assert.Equal(standard.ToArray(), pooled.ToArray());
    }

    [Fact]
    public void BehaviorMatchesMemoryStream_SeekAndRead()
    {
        var data = new byte[] { 10, 20, 30, 40, 50 };

        using var pooled = new PooledMemoryStream();
        using var standard = new MemoryStream();

        pooled.Write(data, 0, data.Length);
        standard.Write(data, 0, data.Length);

        // 中間からシーク
        pooled.Seek(-3, SeekOrigin.End);
        standard.Seek(-3, SeekOrigin.End);

        Assert.Equal(standard.Position, pooled.Position);

        var pbuf = new byte[3];
        var sbuf = new byte[3];
        pooled.ReadExactly(pbuf, 0, 3);
        standard.ReadExactly(sbuf, 0, 3);

        Assert.Equal(sbuf, pbuf);
    }

    [Fact]
    public void BehaviorMatchesMemoryStream_SetLength()
    {
        using var pooled = new PooledMemoryStream();
        using var standard = new MemoryStream();

        pooled.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
        standard.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);

        // 縮小
        pooled.SetLength(3);
        standard.SetLength(3);

        Assert.Equal(standard.Length, pooled.Length);
        Assert.Equal(standard.Position, pooled.Position);

        // 拡大（ゼロパディング）
        pooled.SetLength(6);
        standard.SetLength(6);

        pooled.Position = 0;
        standard.Position = 0;

        Assert.Equal(standard.ToArray(), pooled.ToArray());
    }


    // ════════════════════════════════════════════════════════════════════════
    // Flush
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var ms = new PooledMemoryStream();
        ms.Write(new byte[] { 1, 2, 3 }, 0, 3);
        ms.Flush(); // 例外なし
    }

    // ════════════════════════════════════════════════════════════════════════
    // 大量データ書き込み
    // ════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(1_000)]
    [InlineData(64_000)]
    [InlineData(1_000_000)]
    public void LargeData_WriteAndRead_Consistent(int size)
    {
        using var ms = new PooledMemoryStream();

        var data = new byte[size];
        for (var i = 0; i < size; i++)
            data[i] = (byte)(i % 256);

        ms.Write(data, 0, data.Length);

        Assert.Equal((long)size, ms.Length);

        ms.Position = 0;
        var result = ms.ToArray();

        Assert.Equal(data, result);
    }

    // ════════════════════════════════════════════════════════════════════════
    // using ステートメントとの相互運用
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UsingStatement_DisposesCorrectly()
    {
        PooledMemoryStream? capturedRef;
        using (var ms = new PooledMemoryStream())
        {
            capturedRef = ms;
            ms.Write(new byte[] { 1, 2, 3 }, 0, 3);
        }

        // Dispose 後にアクセスすると例外
        Assert.Throws<ObjectDisposedException>(() => capturedRef.WriteByte(0));
    }

    // ════════════════════════════════════════════════════════════════════════
    // BinaryReader / BinaryWriter との統合
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Interop_BinaryWriter_BinaryReader()
    {
        using var ms = new PooledMemoryStream();

        using (var writer = new System.IO.BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(42);
            writer.Write(3.14f);
            writer.Write("hello");
        }

        ms.Position = 0;

        using var reader = new System.IO.BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
        Assert.Equal(42, reader.ReadInt32());
        Assert.Equal(3.14f, reader.ReadSingle());
        Assert.Equal("hello", reader.ReadString());
    }

    // ════════════════════════════════════════════════════════════════════════
    // Stream.CopyTo との統合
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CopyTo_WorksCorrectly()
    {
        using var source = new PooledMemoryStream();
        var data = new byte[] { 7, 14, 21, 28 };
        source.Write(data, 0, data.Length);
        source.Position = 0;

        using var dest = new PooledMemoryStream();
        source.CopyTo(dest);

        Assert.Equal(data, dest.ToArray());
    }
}
