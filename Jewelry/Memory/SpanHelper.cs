using System;

namespace Jewelry.Memory;

public static class SpanHelper
{
    public static int LowerBound<T>(ReadOnlySpan<T> a, T v, Comparison<T> cmp)
    {
        var l = 0;
        var r = a.Length - 1;

        while (l <= r)
        {
            var mid = l + (r - l) / 2;
            var res = cmp(a[mid], v);

            if (res == -1)
                l = mid + 1;
            else
                r = mid - 1;
        }

        return l;
    }

    public static int UpperBound<T>(ReadOnlySpan<T> a, T v, Comparison<T> cmp)
    {
        var l = 0;
        var r = a.Length - 1;

        while (l <= r)
        {
            var mid = l + (r - l) / 2;
            var res = cmp(a[mid], v);

            if (res <= 0)
                l = mid + 1;
            else
                r = mid - 1;
        }

        return l;
    }
}