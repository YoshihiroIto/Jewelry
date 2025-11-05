using System;
using System.Collections.Generic;
using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory;

public class SpanHelperTest
{
    [Fact]
    public void LowerBound_Empty()
    {
        var a = Array.Empty<int>();
        Assert.Equal(0, SpanHelper.LowerBound(a.AsSpan(), 0, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void LowerBound_OneElement_Found()
    {
        var a = new[] { 1 };
        Assert.Equal(0, SpanHelper.LowerBound(a.AsSpan(), 1, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void LowerBound_OneElement_NotFound()
    {
        var a = new[] { 1 };
        Assert.Equal(0, SpanHelper.LowerBound(a.AsSpan(), 0, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(1, SpanHelper.LowerBound(a.AsSpan(), 2, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void LowerBound_MultipleElements()
    {
        var a = new[] { 1, 3, 5, 7, 9 };
        Assert.Equal(0, SpanHelper.LowerBound(a.AsSpan(), 0, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(0, SpanHelper.LowerBound(a.AsSpan(), 1, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(1, SpanHelper.LowerBound(a.AsSpan(), 2, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(1, SpanHelper.LowerBound(a.AsSpan(), 3, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(2, SpanHelper.LowerBound(a.AsSpan(), 4, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(2, SpanHelper.LowerBound(a.AsSpan(), 5, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(3, SpanHelper.LowerBound(a.AsSpan(), 6, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(3, SpanHelper.LowerBound(a.AsSpan(), 7, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(4, SpanHelper.LowerBound(a.AsSpan(), 8, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(4, SpanHelper.LowerBound(a.AsSpan(), 9, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(5, SpanHelper.LowerBound(a.AsSpan(), 10, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void UpperBound_Empty()
    {
        var a = Array.Empty<int>();
        Assert.Equal(0, SpanHelper.UpperBound(a.AsSpan(), 0, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void UpperBound_OneElement_Found()
    {
        var a = new[] { 1 };
        Assert.Equal(1, SpanHelper.UpperBound(a.AsSpan(), 1, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void UpperBound_OneElement_NotFound()
    {
        var a = new[] { 1 };
        Assert.Equal(0, SpanHelper.UpperBound(a.AsSpan(), 0, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(1, SpanHelper.UpperBound(a.AsSpan(), 2, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void UpperBound_MultipleElements()
    {
        var a = new[] { 1, 3, 5, 7, 9 };
        Assert.Equal(0, SpanHelper.UpperBound(a.AsSpan(), 0, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(1, SpanHelper.UpperBound(a.AsSpan(), 1, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(1, SpanHelper.UpperBound(a.AsSpan(), 2, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(2, SpanHelper.UpperBound(a.AsSpan(), 3, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(2, SpanHelper.UpperBound(a.AsSpan(), 4, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(3, SpanHelper.UpperBound(a.AsSpan(), 5, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(3, SpanHelper.UpperBound(a.AsSpan(), 6, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(4, SpanHelper.UpperBound(a.AsSpan(), 7, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(4, SpanHelper.UpperBound(a.AsSpan(), 8, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(5, SpanHelper.UpperBound(a.AsSpan(), 9, (x, y) => x < y ? -1 : x > y ? 1 : 0));
        Assert.Equal(5, SpanHelper.UpperBound(a.AsSpan(), 10, (x, y) => x < y ? -1 : x > y ? 1 : 0));
    }

    [Fact]
    public void LowerBound_MultipleElements_Comparer()
    {
        var a = new[] { 1, 3, 5, 7, 9 };
        Assert.Equal(0, SpanHelper.LowerBound(a.AsSpan(), 0, Comparer<int>.Default));
        Assert.Equal(0, SpanHelper.LowerBound(a.AsSpan(), 1, Comparer<int>.Default));
        Assert.Equal(1, SpanHelper.LowerBound(a.AsSpan(), 2, Comparer<int>.Default));
        Assert.Equal(1, SpanHelper.LowerBound(a.AsSpan(), 3, Comparer<int>.Default));
        Assert.Equal(2, SpanHelper.LowerBound(a.AsSpan(), 4, Comparer<int>.Default));
        Assert.Equal(2, SpanHelper.LowerBound(a.AsSpan(), 5, Comparer<int>.Default));
        Assert.Equal(3, SpanHelper.LowerBound(a.AsSpan(), 6, Comparer<int>.Default));
        Assert.Equal(3, SpanHelper.LowerBound(a.AsSpan(), 7, Comparer<int>.Default));
        Assert.Equal(4, SpanHelper.LowerBound(a.AsSpan(), 8, Comparer<int>.Default));
        Assert.Equal(4, SpanHelper.LowerBound(a.AsSpan(), 9, Comparer<int>.Default));
        Assert.Equal(5, SpanHelper.LowerBound(a.AsSpan(), 10, Comparer<int>.Default));
    }

    [Fact]
    public void UpperBound_MultipleElements_Comparer()
    {
        var a = new[] { 1, 3, 5, 7, 9 };
        Assert.Equal(0, SpanHelper.UpperBound(a.AsSpan(), 0, Comparer<int>.Default));
        Assert.Equal(1, SpanHelper.UpperBound(a.AsSpan(), 1, Comparer<int>.Default));
        Assert.Equal(1, SpanHelper.UpperBound(a.AsSpan(), 2, Comparer<int>.Default));
        Assert.Equal(2, SpanHelper.UpperBound(a.AsSpan(), 3, Comparer<int>.Default));
        Assert.Equal(2, SpanHelper.UpperBound(a.AsSpan(), 4, Comparer<int>.Default));
        Assert.Equal(3, SpanHelper.UpperBound(a.AsSpan(), 5, Comparer<int>.Default));
        Assert.Equal(3, SpanHelper.UpperBound(a.AsSpan(), 6, Comparer<int>.Default));
        Assert.Equal(4, SpanHelper.UpperBound(a.AsSpan(), 7, Comparer<int>.Default));
        Assert.Equal(4, SpanHelper.UpperBound(a.AsSpan(), 8, Comparer<int>.Default));
        Assert.Equal(5, SpanHelper.UpperBound(a.AsSpan(), 9, Comparer<int>.Default));
        Assert.Equal(5, SpanHelper.UpperBound(a.AsSpan(), 10, Comparer<int>.Default));
    }


    [Fact]
    public void LowerBound_StringChar_Simple()
    {
        var a = new[] { "apple", "banana", "grape" };
        
        Assert.Equal(0, SpanHelper.LowerBound<string, char>(a.AsSpan(), 'a', CompareStringChar));
        Assert.Equal(1, SpanHelper.LowerBound<string, char>(a.AsSpan(), 'b', CompareStringChar));
        Assert.Equal(2, SpanHelper.LowerBound<string, char>(a.AsSpan(), 'g', CompareStringChar));
        Assert.Equal(2, SpanHelper.LowerBound<string, char>(a.AsSpan(), 'f', CompareStringChar)); // Insertion point for 'f' is before 'grape'
        Assert.Equal(3, SpanHelper.LowerBound<string, char>(a.AsSpan(), 'z', CompareStringChar)); // Insertion point for 'z' is at the end
        
        return;

        static int CompareStringChar(string x, char y)
        {
            if (x[0] < y)
                return -1;
            if (x[0] == y)
                return 0;
            return 1;
        }
    }
}