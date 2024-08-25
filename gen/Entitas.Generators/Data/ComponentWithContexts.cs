using System;
using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public readonly struct ComponentWithContexts : IEquatable<ComponentWithContexts>
{
    public readonly ComponentData ComponentData;
    public readonly ImmutableArray<ContextData> ContextDatas;

    public ComponentWithContexts(ComponentData componentData, ImmutableArray<ContextData> contextDatas)
    {
        ComponentData = componentData;
        ContextDatas = contextDatas;
    }

    public bool Equals(ComponentWithContexts other)
    {
        return ComponentData.Equals(other.ComponentData) && ContextDatas.Equals(other.ContextDatas);
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentWithContexts other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (ComponentData.GetHashCode() * 397) ^ ContextDatas.GetHashCode();
        }
    }
}
