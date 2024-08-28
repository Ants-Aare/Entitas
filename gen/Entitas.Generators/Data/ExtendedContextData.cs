using System;
using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public readonly struct ExtendedContextData : IEquatable<ExtendedContextData>
{
    public readonly ContextData ContextData;
    public readonly ImmutableArray<ComponentData> ComponentDatas;
    public readonly ImmutableArray<SystemData> SystemDatas;

    public ExtendedContextData(ContextData contextData, ImmutableArray<ComponentData> componentDatas, ImmutableArray<SystemData> systemDatas)
    {
        ContextData = contextData;
        ComponentDatas = componentDatas;
        SystemDatas = systemDatas;
    }

    public bool Equals(ExtendedContextData other)
    {
        return ContextData.Equals(other.ContextData) && ComponentDatas.Equals(other.ComponentDatas);
    }

    public override bool Equals(object? obj)
    {
        return obj is ExtendedContextData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (ContextData.GetHashCode() * 397) ^ ComponentDatas.GetHashCode();
        }
    }
}
