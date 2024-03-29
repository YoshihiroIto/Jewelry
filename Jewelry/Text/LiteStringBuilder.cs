﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Based code
// https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Text/ValueStringBuilder.cs

namespace Jewelry.Text;

public ref struct LiteStringBuilder
{
    private char[]? _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public LiteStringBuilder(Span<char> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    public LiteStringBuilder(int initialCapacity)
    {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
        _pos = 0;
    }

    public int Length
    {
        get => _pos;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _chars.Length);
            _pos = value;
        }
    }

    public int Capacity => _chars.Length;

    public void EnsureCapacity(int capacity)
    {
        if (capacity > _chars.Length)
            Grow(capacity - _pos);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)"
    /// </summary>
    public ref char GetPinnableReference()
    {
        return ref MemoryMarshal.GetReference(_chars);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }

        return ref MemoryMarshal.GetReference(_chars);
    }

    public ref char this[int index]
    {
        get
        {
            Debug.Assert(index < _pos);
            return ref _chars[index];
        }
    }

    public override string ToString()
    {
        var s = _chars[.._pos].ToString();
        Dispose();
        return s;
    }

    public string ToStringWithoutLastNewLine()
    {
        if (_pos >= Environment.NewLine.Length)
        {
            var lastNewLine = _chars.Slice(_pos - Environment.NewLine.Length, Environment.NewLine.Length);

            if (lastNewLine.SequenceEqual(Environment.NewLine.AsSpan()))
            {
                var s = _chars[..(_pos - Environment.NewLine.Length)].ToString();
                Dispose();
                return s;
            }
        }

        return ToString();
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }

        return _chars[.._pos];
    }

    public ReadOnlySpan<char> AsSpan() => _chars[.._pos];
    public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);
    public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        if (_chars[.._pos].TryCopyTo(destination))
        {
            charsWritten = _pos;
            Dispose();
            return true;
        }
        else
        {
            charsWritten = 0;
            Dispose();
            return false;
        }
    }

    public void Insert(int index, char value, int count)
    {
        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        int remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        _chars.Slice(index, count).Fill(value);
        _pos += count;
    }

    public void Insert(int index, string s)
    {
        int count = s.Length;

        if (_pos > (_chars.Length - count))
        {
            Grow(count);
        }

        int remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars[(index + count)..]);
        s.AsSpan().CopyTo(_chars[index..]);
        _pos += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        int pos = _pos;
        if ((uint)pos < (uint)_chars.Length)
        {
            _chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string s)
    {
        int pos = _pos;
        if (s.Length == 1 &&
            (uint)pos < (uint)_chars
                .Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
        {
            AppendSlow(s);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(IEnumerable<string> ss)
    {
        foreach (var s in ss)
            Append(s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(string s, bool putEmptyNewLine = true)
    {
        Append(s);

        if (putEmptyNewLine || string.IsNullOrEmpty(s) == false)
            AppendNewLine();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(IEnumerable<string> ss, bool putEmptyNewLine = true)
    {
        var isEmpty = true;
        foreach (var s in ss)
        {
            Append(s);
            isEmpty = false;
        }

        if (putEmptyNewLine || isEmpty == false)
            AppendNewLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendIfNotNull(string? s)
    {
        if (s is null)
            return;

        Append(s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLineIfNotNull(string? s)
    {
        if (s is not null)
            Append(s);

        AppendNewLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendNewLine()
    {
        Append(Environment.NewLine);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(short v, ReadOnlySpan<char> format = default)
    {
        retry:
        if (v.TryFormat(_chars[_pos..], out var charsWritten, format) == false)
        {
            Grow(32);
            goto retry;
        }

        _pos += charsWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(int v, ReadOnlySpan<char> format = default)
    {
        retry:
        if (v.TryFormat(_chars[_pos..], out var charsWritten, format) == false)
        {
            Grow(32);
            goto retry;
        }

        _pos += charsWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(float v, ReadOnlySpan<char> format = default)
    {
        retry:
        if (v.TryFormat(_chars[_pos..], out var charsWritten, format) == false)
        {
            Grow(32);
            goto retry;
        }

        _pos += charsWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(double v, ReadOnlySpan<char> format = default)
    {
        retry:
        if (v.TryFormat(_chars[_pos..], out var charsWritten, format) == false)
        {
            Grow(32);
            goto retry;
        }

        _pos += charsWritten;
    }

    public void Append(char c, int count)
    {
        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        Span<char> dst = _chars.Slice(_pos, count);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = c;
        }

        _pos += count;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        int pos = _pos;
        if (pos > _chars.Length - value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_chars[_pos..]);
        _pos += value.Length;
    }
    
    public void AppendLine(ReadOnlySpan<char> value)
    {
        Append(value);
        AppendNewLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        int origPos = _pos;
        if (origPos > _chars.Length - length)
        {
            Grow(length);
        }

        _pos = origPos + length;
        return _chars.Slice(origPos, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

    private void AppendSlow(string s)
    {
        int pos = _pos;
        if (pos > _chars.Length - s.Length)
        {
            Grow(s.Length);
        }

        s.AsSpan().CopyTo(_chars[pos..]);
        _pos += s.Length;
    }

    /// <summary>
    /// Resize the internal buffer either by doubling current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondPos"/> to
    /// <see cref="_pos"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        Debug.Assert(additionalCapacityBeyondPos > 0);
        Debug.Assert(_pos > _chars.Length - additionalCapacityBeyondPos,
            "Grow called incorrectly, no resize is needed.");

        char[] poolArray = ArrayPool<char>.Shared.Rent(Math.Max(_pos + additionalCapacityBeyondPos, _chars.Length * 2));

        _chars.CopyTo(poolArray);

        char[]? toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        char[]? toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }
}