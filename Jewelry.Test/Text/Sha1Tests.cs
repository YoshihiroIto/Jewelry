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