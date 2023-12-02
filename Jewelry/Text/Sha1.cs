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
        return $"{_value0:x32}{_value1:x8}";
    }
}