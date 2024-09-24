using System.Collections.Immutable;

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
}
