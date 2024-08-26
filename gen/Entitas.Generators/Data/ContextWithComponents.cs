using System;
using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public readonly struct ContextWithComponents : IEquatable<ContextWithComponents>
{
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

    public readonly ContextData ContextData;
    public readonly ImmutableArray<ComponentData> ComponentDatas;
    public ContextWithComponents(ContextData contextData, ImmutableArray<ComponentData> componentDatas)
    {
        ContextData = contextData;
        ComponentDatas = componentDatas;
    }
}
