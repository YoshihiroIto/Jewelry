using System.Diagnostics.CodeAnalysis;
using System.Text;
using Jewelry.Text;
using System;
using Xunit;

// ReSharper disable StringLiteralTypo

namespace Jewelry.Test.Text;

[SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
public class LiteStringBuilderTest
{
    [Fact]
    public void Smoke()
    {
        using var lsb = new LiteStringBuilder();
    }

    [Fact]
    public void Simple()
    {
        using var lsb = new LiteStringBuilder();

        lsb.Append("aaa");
        lsb.Append("bbb");

        Assert.Equal("aaabbb", lsb.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void StackSimple(int size)
    {
        using var lsb = new LiteStringBuilder(stackalloc char[size]);

        lsb.Append("aaa");
        lsb.Append("bbb");

        Assert.Equal("aaabbb", lsb.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void ArrayPoolSimple(int size)
    {
        using var lsb = new LiteStringBuilder(size);

        lsb.Append("aaa");
        lsb.Append("bbb");

        Assert.Equal("aaabbb", lsb.ToString());
    }

    [Fact]
    public void AppendIfNotNull()
    {
        using var lsb = new LiteStringBuilder();

        lsb.AppendIfNotNull("aaa");
        lsb.AppendIfNotNull(null);
        lsb.AppendIfNotNull("bbb");

        Assert.Equal("aaabbb", lsb.ToString());


        var a = new StringBuilder();

        a.AppendLine("aaa");
    }

    [Fact]
    public void AppendLine()
    {
        using var lsb = new LiteStringBuilder();

        lsb.AppendLine("aaa");
        lsb.AppendLine("bbb");

        Assert.Equal($"aaa{Environment.NewLine}bbb{Environment.NewLine}", lsb.ToString());
    }

    [Fact]
    public void AppendLineIfNotNull()
    {
        using var lsb = new LiteStringBuilder();

        lsb.AppendLine("aaa");
        lsb.AppendLineIfNotNull(null);
        lsb.AppendLine("bbb");

        Assert.Equal($"aaa{Environment.NewLine}{Environment.NewLine}bbb{Environment.NewLine}", lsb.ToString());
    }

    [Fact]
    public void ToStringWithoutLastNewLine()
    {
        using var lsb = new LiteStringBuilder();

        lsb.AppendLine("aaa");
        lsb.AppendLineIfNotNull(null);
        lsb.AppendLine("bbb");

        Assert.Equal($"aaa{Environment.NewLine}{Environment.NewLine}bbb", lsb.ToStringWithoutLastNewLine());
    }
    
    [Fact]
    public void ToStringWithoutLastNewLine_NoNewLine()
    {
        using var lsb = new LiteStringBuilder();

        lsb.Append($"aaa{Environment.NewLine}bbb");

        Assert.Equal($"aaa{Environment.NewLine}bbb", lsb.ToStringWithoutLastNewLine());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void AppendInt(int size)
    {
        using var lsb = new LiteStringBuilder(stackalloc char[size]);

        lsb.Append(1);
        lsb.Append(234);
        lsb.Append(56);

        Assert.Equal("123456", lsb.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void AppendIntFormat(int size)
    {
        using var lsb = new LiteStringBuilder(stackalloc char[size]);

        lsb.Append(1, "D4".AsSpan());

        Assert.Equal("0001", lsb.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void AppendDouble(int size)
    {
        using var lsb = new LiteStringBuilder(stackalloc char[size]);

        lsb.Append(1.5);
        lsb.Append(2);
        lsb.Append(56);

        Assert.Equal("1.5256", lsb.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void AppendDoubleFormat(int size)
    {
        using var lsb = new LiteStringBuilder(stackalloc char[size]);

        lsb.Append(1.5, "F3".AsSpan());

        Assert.Equal("1.500", lsb.ToString());
    }
}