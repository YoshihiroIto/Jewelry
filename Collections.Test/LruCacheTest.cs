using Jewelry.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Collections.Test
{
    [TestClass]
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
        
        [TestMethod]
        public void SimpleMethod()
        {
            var cache = new StringLruCache();
 
            cache.Add("key0", "0123456789");

            Assert.AreEqual(cache.Get("key0"), "0123456789");
            Assert.AreEqual(cache.Get("key1"), null);

            cache.Add("key1", "ABCDEFGHIJ");

            Assert.AreEqual(cache.Get("key0"), "0123456789");
            Assert.AreEqual(cache.Get("key1"), "ABCDEFGHIJ");

            cache.Add("key2", "1122334455");

            Assert.AreEqual(cache.Get("key0"), null);
            Assert.AreEqual(cache.Get("key1"), "ABCDEFGHIJ");
            Assert.AreEqual(cache.Get("key2"), "1122334455");
            Assert.AreEqual(cache.DiscardedValueCount, 1);

            // ReSharper disable once UnusedVariable
            var touch = cache.Get("key1");
            cache.Add("key3", "6677889900");

            Assert.AreEqual(cache.Get("key0"), null);
            Assert.AreEqual(cache.Get("key1"), "ABCDEFGHIJ");
            Assert.AreEqual(cache.Get("key2"), null);
            Assert.AreEqual(cache.Get("key3"), "6677889900");
        }
    }
}
