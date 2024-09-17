using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public static class IncrementalGeneratorExtensions
{
    public static IncrementalValuesProvider<T> RemoveEmptyValues<T>(this IncrementalValuesProvider<T?> valuesProvider) where T : struct
        => valuesProvider.Where(x => x.HasValue).Select((x, _) => x!.Value);

    public static IncrementalValuesProvider<T> RemoveDuplicates<T>(this IncrementalValuesProvider<T> valuesProvider) where T : struct
        => valuesProvider.Collect().SelectMany((x, _) => x.Distinct());

    public static IncrementalValuesProvider<T> RemoveDuplicates<T>(this IncrementalValuesProvider<T> valuesProvider, IEqualityComparer<T> equalityComparer) where T : struct
        => valuesProvider.Collect().SelectMany((x, _) => x.Distinct(equalityComparer));

    public static IncrementalValuesProvider<T> Append<T>(this IncrementalValuesProvider<T> valuesProvider, IncrementalValuesProvider<T> other) where T : struct
        => valuesProvider.Collect().Combine(other.Collect()).SelectMany((x, _) => x.Left.AddRange(x.Right));

    public static IncrementalValueProvider<ImmutableArray<T>> Append<T>(this IncrementalValueProvider<ImmutableArray<T>> valueProvider, IncrementalValueProvider<ImmutableArray<T>> other) where T : struct
        => valueProvider.Combine(other).Select((tuple, _) => tuple.Left.AddRange(tuple.Right));
    public static IncrementalValuesProvider<T> AppendWithoutDuplicates<T>(this IncrementalValuesProvider<T> valuesProvider, IncrementalValuesProvider<T> other) where T : struct
        => valuesProvider.Collect().Combine(other.Collect()).SelectMany((x, _) => x.Left.AddRange(x.Right).Distinct());

    public static IncrementalValueProvider<ImmutableArray<T>> Sort<T>(this IncrementalValueProvider<ImmutableArray<T>> valueProvider)
        => valueProvider.Select(static (x, _) => x.Sort());

    public static IncrementalValueProvider<ImmutableArray<T>> Sort<T>(this IncrementalValueProvider<ImmutableArray<T>> valueProvider, IComparer<T> iComparer)
        => valueProvider.Select((x, _) => x.Sort(iComparer));

    public static IncrementalValueProvider<ImmutableArray<T>> Sort<T>(this IncrementalValueProvider<ImmutableArray<T>> valueProvider, Comparison<T> comparison)
        => valueProvider.Select((x, _) => x.Sort(comparison));

    public static IncrementalValuesProvider<TOut> SelectManyWithIndex<TIn, TOut>(this IncrementalValueProvider<ImmutableArray<TIn>> valueProvider, Func<TIn, int, TOut> transform)
        => valueProvider.SelectMany((x, _) => x.Select(transform));
}
