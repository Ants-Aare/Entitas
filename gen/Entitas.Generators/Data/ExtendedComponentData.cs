using System;
using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public readonly struct ExtendedComponentData : IEquatable<ExtendedComponentData>
{
    public readonly ComponentData ComponentData;
    public readonly ImmutableArray<ContextData> ContextDatas;

    public ExtendedComponentData(ComponentData componentData, ImmutableArray<ContextData> contextDatas)
    {
        ComponentData = componentData;
        ContextDatas = contextDatas;
    }

    public bool Equals(ExtendedComponentData other)
    {
        return ComponentData.Equals(other.ComponentData) && ContextDatas.Equals(other.ContextDatas);
    }

    public override bool Equals(object? obj)
    {
        return obj is ExtendedComponentData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (ComponentData.GetHashCode() * 397) ^ ContextDatas.GetHashCode();
        }
    }
}
