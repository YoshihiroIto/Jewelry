using Jewelry.Memory;
using Xunit;

namespace Jewelry.Test.Memory
{
    public class FixedBufferTest
    {
        [Fact]
        public void Smoke()
        {
            using var fb = new FixedBuffer<int>(100);
        }
        
        [Fact]
        public void DefaultCtor()
        {
            using var fb = new FixedBuffer<int>(0);
        }
    }
}