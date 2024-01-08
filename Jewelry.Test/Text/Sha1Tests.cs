using Xunit;
using Jewelry.Text;
using System;
using System.Collections.Generic;

namespace Jewelry.Test.Text;

public sealed class Sha1Tests
{
    [Fact]
    public void Parse()
    {
        var sut = new Sha1("a78ca3a164cebb516a6fda68b655b7903a895209");

        var result = sut.ToString();

        Assert.Equal("a78ca3a164cebb516a6fda68b655b7903a895209", result);
    }

    [Theory]
    [MemberData(nameof(ToShortStringSource))]
    public void ToShortString(string expected, int length)

    {
        var sut = new Sha1("a78ca3a164cebb516a6fda68b655b7903a895209");

        var result = sut.ToShortString(length);

        Assert.Equal(expected, result);
    }

    public static IEnumerable<object[]> ToShortStringSource()
    {
        for (var i = 0; i <= 40; ++i)
            yield return ["a78ca3a164cebb516a6fda68b655b7903a895209"[..i], i];
    }

    [Fact]
    public void ToShortString_Under()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var sut = new Sha1("a78ca3a164cebb516a6fda68b655b7903a895209");
            sut.ToShortString(-1);
        });
    }

    [Fact]
    public void ToShortString_Over()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var sut = new Sha1("a78ca3a164cebb516a6fda68b655b7903a895209");
            sut.ToShortString(100);
        });
    }

    [Fact]
    public void OperatorEquals()
    {
        var sha1 = new Sha1("a78ca3a164cebb516a6fda68b655b7903a895209");
        var sha2 = new Sha1("a78ca3a164cebb516a6fda68b655b7903a895209");

        var result = sha1 == sha2;

        Assert.True(result);
    }

    [Fact]
    public void OperatorNotEquals()
    {
        var sha1 = new Sha1("a78ca3a164cebb516a6fda68b655b7903a895209");
        var sha2 = new Sha1("b78ca3a164cebb516a6fda68b655b7903a895209");

        var result = sha1 != sha2;

        Assert.True(result);
    }
}