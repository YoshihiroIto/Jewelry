using System;
using System.Diagnostics.CodeAnalysis;
using Jewelry.Text;
using Xunit;

namespace Jewelry.Test.Text
{
    [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
    public class StringSplitterTest
    {
        [Fact]
        public void Smoke()
        {
            using var ss = new StringSplitter();
        }

        [Fact]
        public void Simple()
        {
            using var ss = new StringSplitter(64);

            var text = "abc def xyz";

            var es = ss.Split(text, ' ');

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", text.AsSpan().Slice(es[0].Start, es[0].Length).ToString());
            Assert.Equal("def", text.AsSpan().Slice(es[1].Start, es[1].Length).ToString());
            Assert.Equal("xyz", text.AsSpan().Slice(es[2].Start, es[2].Length).ToString());
        }

        [Fact]
        public void SepSep()
        {
            using var ss = new StringSplitter(64);

            var text = "abc  def";

            var es = ss.Split(text, ' ');

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", text.AsSpan().Slice(es[0].Start, es[0].Length).ToString());
            Assert.Equal("", text.AsSpan().Slice(es[1].Start, es[1].Length).ToString());
            Assert.Equal("def", text.AsSpan().Slice(es[2].Start, es[2].Length).ToString());
        }

        [Fact]
        public void StartSep1()
        {
            using var ss = new StringSplitter(64);

            var text = " abc";

            var es = ss.Split(text, ' ');

            Assert.Equal(2, es.Length);
            Assert.Equal("", text.AsSpan().Slice(es[0].Start, es[0].Length).ToString());
            Assert.Equal("abc", text.AsSpan().Slice(es[1].Start, es[1].Length).ToString());
        }

        [Fact]
        public void StartSep2()
        {
            using var ss = new StringSplitter(64);

            var text = "  abc";

            var es = ss.Split(text, ' ');

            Assert.Equal(3, es.Length);
            Assert.Equal("", text.AsSpan().Slice(es[0].Start, es[0].Length).ToString());
            Assert.Equal("", text.AsSpan().Slice(es[1].Start, es[1].Length).ToString());
            Assert.Equal("abc", text.AsSpan().Slice(es[2].Start, es[2].Length).ToString());
        }

        [Fact]
        public void EndSep1()
        {
            using var ss = new StringSplitter(64);

            var text = "abc ";

            var es = ss.Split(text, ' ');

            Assert.Equal(2, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("",    es[1].ToString(text));
        }

        [Fact]
        public void EndSep2()
        {
            using var ss = new StringSplitter(64);

            var text = "abc  ";

            var es = ss.Split(text, ' ');

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("",    es[1].ToString(text));
            Assert.Equal("",    es[2].ToString(text));
        }

        [Fact]
        public void StartEndSep1()
        {
            using var ss = new StringSplitter(64);

            var text = " abc ";

            var es = ss.Split(text, ' ');

            Assert.Equal(3, es.Length);
            Assert.Equal("",    es[0].ToString(text));
            Assert.Equal("abc", es[1].ToString(text));
            Assert.Equal("",    es[2].ToString(text));
        }

        [Fact]
        public void StartEndSep2()
        {
            using var ss = new StringSplitter(64);

            var text = "  abc  ";

            var es = ss.Split(text, ' ');

            Assert.Equal(5, es.Length);
            Assert.Equal("",    es[0].ToString(text));
            Assert.Equal("",    es[1].ToString(text));
            Assert.Equal("abc", es[2].ToString(text));
            Assert.Equal("",    es[3].ToString(text));
            Assert.Equal("",    es[4].ToString(text));
        }

        [Fact]
        public void SimpleRemoveEmptyEntries()
        {
            using var ss = new StringSplitter(64);

            var text = " abc  def    xyz ";

            var es = ss.Split(text, ' ', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("def", es[1].ToString(text));
            Assert.Equal("xyz", es[2].ToString(text));
        }

        [Fact]
        public void StackBuffer()
        {
            using var ss = new StringSplitter(stackalloc StringSplitter.StringSpan[32]);

            var text = " abc  def    xyz ";

            var es = ss.Split(text, ' ', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("def", es[1].ToString(text));
            Assert.Equal("xyz", es[2].ToString(text));
        }

        [Fact]
        public void MiniBuffer()
        {
            using var ss = new StringSplitter(0);

            var text = " abc  def    xyz ";

            var es = ss.Split(text, ' ', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("def", es[1].ToString(text));
            Assert.Equal("xyz", es[2].ToString(text));
        }

        [Fact]
        public void MiniStackBuffer()
        {
            using var ss = new StringSplitter(stackalloc StringSplitter.StringSpan[0]);

            var text = " abc  def    xyz ";

            var es = ss.Split(text, ' ', StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("def", es[1].ToString(text));
            Assert.Equal("xyz", es[2].ToString(text));
        }

        [Fact]
        public void SimpleSeparators()
        {
            using var ss = new StringSplitter(64);

            var text = "abc,def xyz";

            var es = ss.Split(text, " ,");

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("def", es[1].ToString(text));
            Assert.Equal("xyz", es[2].ToString(text));
        }

        [Fact]
        public void SimpleSeparatorsRemoveEmptyEntries()
        {
            using var ss = new StringSplitter(64);

            var text = " , , abc,, , ,def,, xyz,,,  ";

            var es = ss.Split(text, " ,", StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(3, es.Length);
            Assert.Equal("abc", es[0].ToString(text));
            Assert.Equal("def", es[1].ToString(text));
            Assert.Equal("xyz", es[2].ToString(text));
        }

        [Fact]
        public void Null()
        {
            using var ss = new StringSplitter(64);

            var es = ss.Split(null, " ,", StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(0, es.Length);
        }
    }
}