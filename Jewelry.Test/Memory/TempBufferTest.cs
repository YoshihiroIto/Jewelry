using System.Diagnostics.CodeAnalysis;
using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory
{
    [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
    public class TempBufferTest
    {
        [Fact]
        public void Smoke()
        {
            using var tb = new TempBuffer<int>();
        }

        [Fact]
        public void Simple()
        {
            using var tb = new TempBuffer<int>();

            Assert.Equal(0, tb.Length);

            tb.Add(0);
            tb.Add(1);
            tb.Add(2);

            Assert.Equal(3, tb.Length);
            Assert.Equal(0, tb[0]);
            Assert.Equal(1, tb[1]);
            Assert.Equal(2, tb[2]);

            var b = tb.Buffer;

            Assert.Equal(3, b.Length);
            Assert.Equal(0, b[0]);
            Assert.Equal(1, b[1]);
            Assert.Equal(2, b[2]);

            tb[0] = 999;
            tb[1] = 888;
            tb[2] = 777;

            Assert.Equal(999, tb[0]);
            Assert.Equal(888, tb[1]);
            Assert.Equal(777, tb[2]);

            Assert.Equal(999, b[0]);
            Assert.Equal(888, b[1]);
            Assert.Equal(777, b[2]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void StackSimple(int size)
        {
            using var tb = new TempBuffer<int>(stackalloc int[size]);

            Assert.Equal(0, tb.Length);

            tb.Add(0);
            tb.Add(1);
            tb.Add(2);

            Assert.Equal(3, tb.Length);
            Assert.Equal(0, tb[0]);
            Assert.Equal(1, tb[1]);
            Assert.Equal(2, tb[2]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public void ArrayPoolSimple(int size)
        {
            using var tb = new TempBuffer<int>(size);

            Assert.Equal(0, tb.Length);

            tb.Add(0);
            tb.Add(1);
            tb.Add(2);

            Assert.Equal(3, tb.Length);
            Assert.Equal(0, tb[0]);
            Assert.Equal(1, tb[1]);
            Assert.Equal(2, tb[2]);
        }

        [Fact]
        public void StackGlow()
        {
            using var tb = new TempBuffer<int>(stackalloc int[0]);

            for(var i = 0;i != 1024;++ i)
                tb.Add(i);

            Assert.Equal(1024, tb.Length);
        }

        [Fact]
        public void AllayPoolGlow()
        {
            using var tb = new TempBuffer<int>(0);

            for(var i = 0;i != 1024;++ i)
                tb.Add(i);

            Assert.Equal(1024, tb.Length);
        }
    }
}