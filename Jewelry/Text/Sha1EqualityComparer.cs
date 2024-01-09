using System.Collections.Generic;

namespace Jewelry.Text;

public sealed  class Sha1EqualityComparer : IEqualityComparer<Sha1>
{
    public static readonly Sha1EqualityComparer Shared = new();
    
    public bool Equals(Sha1 x, Sha1 y)
    {
        return x == y;
    }

    public int GetHashCode(Sha1 obj)
    {
        return obj.GetHashCode();
    }
}