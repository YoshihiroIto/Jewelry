using Jewelry.Collections;
using System;
using System.Linq;
using Xunit;

namespace Jewelry.Test.Collections;

public sealed class CompactListTests
{
    [Fact]
    public void Single_value()
    {
        var sut = new CompactList<int> { 123 };

        Assert.Equal(1, sut.Count);
        Assert.Equal(123, sut[0]);
        Assert.True(sut.SequenceEqual(new[] { 123 }));
        Assert.Throws<IndexOutOfRangeException>(() => _ = sut[1]);
    }

    [Fact]
    public void Multiple_values()
    {
        var sut = new CompactList<int> { 123, 456, 789 };

        Assert.Equal(3, sut.Count);
        Assert.Equal(123, sut[0]);
        Assert.Equal(456, sut[1]);
        Assert.Equal(789, sut[2]);
        Assert.True(sut.SequenceEqual(new[] { 123, 456, 789 }));
        Assert.Throws<IndexOutOfRangeException>(() => _ = sut[3]);
    }
    
    [Fact]
    public void Empty()
    {
        var sut = new CompactList<int>();

        Assert.Equal(0, sut.Count);
        Assert.True(sut.SequenceEqual(new int[] { }));
        Assert.Throws<IndexOutOfRangeException>(() => _ = sut[0]);
    }
    
    [Fact]
    public void Add_single_value()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var sut = new CompactList<int>();
        
        sut.Add(123);

        Assert.Equal(1, sut.Count);
        Assert.Equal(123, sut[0]);
        Assert.True(sut.SequenceEqual(new[] { 123 }));
        Assert.Throws<IndexOutOfRangeException>(() => _ = sut[1]);
    }
    
    [Fact]
    public void Add_multiple_value()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var sut = new CompactList<int>();
        
        sut.Add(123);
        sut.Add(456);
        sut.Add(789);

        Assert.Equal(3, sut.Count);
        Assert.Equal(123, sut[0]);
        Assert.Equal(456, sut[1]);
        Assert.Equal(789, sut[2]);
        Assert.True(sut.SequenceEqual(new[] { 123, 456, 789 }));
        Assert.Throws<IndexOutOfRangeException>(() => _ = sut[3]);
    }
    
    [Fact]
    public void Clear()
    {
        var sut = new CompactList<int> { 123, 456, 789 };
        
        sut.Clear();

        Assert.Equal(0, sut.Count);
        Assert.True(sut.SequenceEqual(new int[] { }));
        Assert.Throws<IndexOutOfRangeException>(() => _ = sut[0]);
    }
    
    [Fact]
    public void ToCompactList()
    {
        var sut = new[]{ 123, 456, 789 }.ToCompactList();

        Assert.Equal(3, sut.Count);
        Assert.Equal(123, sut[0]);
        Assert.Equal(456, sut[1]);
        Assert.Equal(789, sut[2]);
        Assert.True(sut.SequenceEqual(new[] { 123, 456, 789 }));
        Assert.Throws<IndexOutOfRangeException>(() => _ = sut[3]);
    }
    
}