using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jewelry.Memory
{
    public ref struct FixedBuffer<T>
    {
        public readonly int Length;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(index < Length);
                return ref _buffer[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(in FixedBuffer<T> x)
        {
            return x._buffer.AsSpan(0, x.Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(in FixedBuffer<T> x)
        {
            return x._buffer.AsSpan(0, x.Length);
        }

        private readonly T[]? _buffer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedBuffer(int length)
        {
            Length = length;
            _buffer = ArrayPool<T>.Shared.Rent(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            var buffer = _buffer;
            this = default;

            if (buffer != null)
                ArrayPool<T>.Shared.Return(buffer);
        }
    }
}