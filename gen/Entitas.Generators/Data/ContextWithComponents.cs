using System;
using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public readonly struct ContextWithComponents : IEquatable<ContextWithComponents>
{
    public readonly ContextData ContextData;
    public readonly ImmutableArray<ComponentData> ComponentDatas;
    public readonly ImmutableArray<SystemData> SystemDatas;

    public ContextWithComponents(ContextData contextData, ImmutableArray<ComponentData> componentDatas, ImmutableArray<SystemData> systemDatas)
    {
        ContextData = contextData;
        ComponentDatas = componentDatas;
        SystemDatas = systemDatas;
    }

    public bool Equals(ContextWithComponents other)
    {
        return ContextData.Equals(other.ContextData) && ComponentDatas.Equals(other.ComponentDatas);
    }

    public override bool Equals(object? obj)
    {
        return obj is ContextWithComponents other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (ContextData.GetHashCode() * 397) ^ ComponentDatas.GetHashCode();
        }
    }
}
