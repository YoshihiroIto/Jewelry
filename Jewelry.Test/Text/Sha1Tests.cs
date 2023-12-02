using Xunit;
using Jewelry.Text;

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
}