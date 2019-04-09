using Jewelry.Collections;
using Xunit;

// ReSharper disable StringLiteralTypo

namespace Jewelry.Test.Collections
{
    public class LruCacheTest
    {
        public class StringLruCache : LruCache<string, string>
        {
            public int DiscardedValueCount
            {
                get;
                set;
            }
            
            public StringLruCache() : base(25, true)
            {
            }

            protected override int GetValueSize(string value)
            {
                return value.Length;
            }

            protected override void OnDiscardedValue(string key, string value)
            {
                ++ DiscardedValueCount;
            }
        }
        
        [Fact]
        public void Basic()
        {
            var cache = new StringLruCache();
 
            cache.Add("key0", "0123456789");

            Assert.Equal( "0123456789", cache.Get("key0"));
            Assert.Null(cache.Get("key1"));

            cache.Add("key1", "ABCDEFGHIJ");

            Assert.Equal("0123456789", cache.Get("key0"));
            Assert.Equal("ABCDEFGHIJ", cache.Get("key1"));

            cache.Add("key2", "1122334455");

            Assert.Null(cache.Get("key0"));
            Assert.Equal("ABCDEFGHIJ", cache.Get("key1"));
            Assert.Equal("1122334455", cache.Get("key2"));
            Assert.Equal(1, cache.DiscardedValueCount);

            // ReSharper disable once UnusedVariable
            var touch = cache.Get("key1");
            cache.Add("key3", "6677889900");

            Assert.Null(cache.Get("key0"));
            Assert.Equal("ABCDEFGHIJ", cache.Get("key1"));
            Assert.Null(cache.Get("key2"));
            Assert.Equal("6677889900", cache.Get("key3"));
        }
    }
}
