using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public record struct ExtendedComponentData(ComponentData ComponentData, ImmutableArray<ContextData> ContextDatas);
public record struct ExtendedComponentDataWithSystemsAndGroups(ComponentData ComponentData, ContextData ContextData, ImmutableArray<SystemData> SystemDatas, ImmutableArray<GroupData> GroupDatas);
public record struct ExtendedGroupData(GroupData GroupData, ImmutableArray<ContextData> ContextDatas);
public record struct ExtendedContextData(ContextData ContextData, ImmutableArray<ComponentData> ComponentDatas, ImmutableArray<SystemData> SystemDatas, ImmutableArray<GroupData> GroupDatas);
public record struct ExtendedArchetypeData(ArchetypeData ArchetypeData, ImmutableArray<ContextData> ContextDatas, ImmutableArray<ComponentData> ComponentDatas);
