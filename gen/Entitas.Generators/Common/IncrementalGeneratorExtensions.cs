using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public static class IncrementalGeneratorExtensions
{
    public static IncrementalValuesProvider<T> RemoveEmptyValues<T>(this IncrementalValuesProvider<T?> valuesProvider) where T : struct
        => valuesProvider.Where(x => x.HasValue).Select((x, _)=> x!.Value);
}
