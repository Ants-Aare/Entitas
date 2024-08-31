using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public record struct ExtendedComponentData(ComponentData ComponentData, ImmutableArray<ContextData> ContextDatas);
