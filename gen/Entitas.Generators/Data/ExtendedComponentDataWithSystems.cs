using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public record struct ExtendedComponentDataWithSystems(ComponentData ComponentData, ContextData ContextData, ImmutableArray<SystemData> SystemDatas);
