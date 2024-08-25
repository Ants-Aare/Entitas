using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public static class IncrementalGeneratorExtensions
{
    public static IncrementalValuesProvider<T> RemoveEmptyValues<T>(this IncrementalValuesProvider<T?> valuesProvider) where T : struct
        => valuesProvider.Where(x => x.HasValue).Select((x, _)=> x!.Value);

    public static IncrementalValueProvider<ImmutableArray<T>> Sort<T>(this IncrementalValueProvider<ImmutableArray<T>> valueProvider)
        => valueProvider.Select(static (x, _) => x.Sort());
    public static IncrementalValueProvider<ImmutableArray<T>> Sort<T>(this IncrementalValueProvider<ImmutableArray<T>> valueProvider, IComparer<T> iComparer)
        => valueProvider.Select((x, _) => x.Sort(iComparer));
}
