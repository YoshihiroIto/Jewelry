using System;
using System.Collections.Generic;

namespace Jewelry.Memory;

public static class SpanHelper
{
    public delegate int Comparison<in T0, in T1>(T0 x, T1 y);
    
    public static int LowerBound<T0, T1>(ReadOnlySpan<T0> a, T1 v, Comparison<T0, T1> cmp)
    {
        var l = 0;
        var r = a.Length - 1;

        while (l <= r)
        {
            var mid = l + (r - l) / 2;
            var res = cmp(a[mid], v);

            if (res < 0)
                l = mid + 1;
            else
                r = mid - 1;
        }

        return l;
    }

    public static int UpperBound<T0, T1>(ReadOnlySpan<T0> a, T1 v, Comparison<T0, T1> cmp)
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

    public static int LowerBound<T>(ReadOnlySpan<T> a, T v, IComparer<T> comparer)
    {
        var l = 0;
        var r = a.Length - 1;

        while (l <= r)
        {
            var mid = l + (r - l) / 2;
            var res = comparer.Compare(a[mid], v);

            if (res < 0)
                l = mid + 1;
            else
                r = mid - 1;
        }

        return l;
    }

    public static int UpperBound<T>(ReadOnlySpan<T> a, T v, IComparer<T> comparer)
    {
        var l = 0;
        var r = a.Length - 1;

        while (l <= r)
        {
            var mid = l + (r - l) / 2;
            var res = comparer.Compare(a[mid], v);

            if (res <= 0)
                l = mid + 1;
            else
                r = mid - 1;
        }

        return l;
    }
}