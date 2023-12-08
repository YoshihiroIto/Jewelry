using System;

namespace Jewelry.Text;

public readonly struct Sha1
{
    private readonly UInt128 _value0;
    private readonly uint _value1;

    public Sha1(string source)
    {
        var s = source.AsSpan();

        _value0 = new UInt128(
            ulong.Parse(s.Slice(0, 16), System.Globalization.NumberStyles.HexNumber),
            ulong.Parse(s.Slice(16, 16), System.Globalization.NumberStyles.HexNumber));
        _value1 = uint.Parse(s.Slice(32, 8), System.Globalization.NumberStyles.HexNumber);
    }

    public override string ToString()
    {
        return string.Create(40, this, (span, t) =>
        {
            for (var i = 0; i != 32; i++)
            {
                var c = (t._value0 >> ((31 - i) * 4)) & 0x0F;
                span[i] = c < 10 ? (char)('0' + c) : (char)('a' + c - 10);
            }

            for (var i = 0; i != 8; i++)
            {
                var c = (t._value1 >> ((7 - i) * 4)) & 0x0F;
                span[32 + i] = c < 10 ? (char)('0' + c) : (char)('a' + c - 10);
            }
        });
    }
}