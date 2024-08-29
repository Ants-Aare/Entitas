using System;
using System.Collections.Immutable;

namespace Entitas.Generators.Data;

public readonly struct ExtendedComponentDataWithSystems : IEquatable<ExtendedComponentDataWithSystems>
{
    public readonly ComponentData ComponentData;
    public readonly ContextData ContextData;
    public readonly ImmutableArray<SystemData> SystemDatas;
    public ExtendedComponentDataWithSystems(ComponentData componentData, ContextData contextData, ImmutableArray<SystemData> systemDatas)
    {
        ComponentData = componentData;
        ContextData = contextData;
        SystemDatas = systemDatas;
    }
    public bool Equals(ExtendedComponentDataWithSystems other)
    {
        return ComponentData.Equals(other.ComponentData) && ContextData.Equals(other.ContextData) && SystemDatas.Equals(other.SystemDatas);
    }

    public override bool Equals(object? obj)
    {
        return obj is ExtendedComponentDataWithSystems other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ComponentData.GetHashCode();
            hashCode = (hashCode * 397) ^ ContextData.GetHashCode();
            hashCode = (hashCode * 397) ^ SystemDatas.GetHashCode();
            return hashCode;
        }
    }
}
