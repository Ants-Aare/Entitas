using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public record struct ExtendedContextData(ContextData ContextData, ImmutableArray<ComponentData> ComponentDatas, ImmutableArray<SystemData> SystemDatas);
