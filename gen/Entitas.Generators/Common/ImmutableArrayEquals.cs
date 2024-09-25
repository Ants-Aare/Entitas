using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Entitas.Generators.Common;

public static class ImmutableArrayEquals
{
    public static int SequenceGetHashCode<T>(this ImmutableArray<T> immutableArray)
    {
        if (immutableArray.IsDefaultOrEmpty)
            return 0;
        var hashCode = 0;

        for (var i = 0; i < immutableArray.Length; i++)
        {
            var itemHashCode = immutableArray[i] == null
                ? 397
                : immutableArray[i]!.GetHashCode();

            unchecked
            {
                hashCode = i == 0
                    ? itemHashCode
                    : (hashCode * 397) ^ itemHashCode;
            }
        }

        return hashCode;
    }

    public static int SequenceGetHashCode<T>(this ImmutableArray<T> immutableArray, IEqualityComparer<T> equalityComparer)
    {
        if (immutableArray.IsDefaultOrEmpty)
            return 0;
        var hashCode = 0;

        for (var i = 0; i < immutableArray.Length; i++)
        {
            var itemHashCode = immutableArray[i] == null
                ? 397
                : equalityComparer.GetHashCode(immutableArray[i]);

            unchecked
            {
                hashCode = i == 0
                    ? itemHashCode
                    : (hashCode * 397) ^ itemHashCode;
            }
        }

        return hashCode;
    }
}

public class ImmutableArrayComparer<T>(IEqualityComparer<T> equalityComparer) : IEqualityComparer<ImmutableArray<T>>
{
    public ImmutableArrayComparer() : this(EqualityComparer<T>.Default) { }
    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y) => ImmutableArrayExtensions.SequenceEqual(x, y, equalityComparer);

    public int GetHashCode(ImmutableArray<T> obj) => obj.SequenceGetHashCode(equalityComparer);
}
