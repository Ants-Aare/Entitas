using System;
using System.Collections.Immutable;
using Entitas.Generators.Data;

namespace Entitas.Generators;

public readonly struct ExtendedSystemData : IEquatable<ExtendedSystemData>
{
    public bool Equals(ExtendedSystemData other)
    {
        return SystemData.Equals(other.SystemData) && ComponentDatas.Equals(other.ComponentDatas);
    }

    public override bool Equals(object? obj)
    {
        return obj is ExtendedSystemData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (SystemData.GetHashCode() * 397) ^ ComponentDatas.GetHashCode();
        }
    }

    public readonly SystemData SystemData;
    public readonly ImmutableArray<ComponentData> ComponentDatas;

    public ExtendedSystemData(SystemData systemData, ImmutableArray<ComponentData> componentDatas)
    {
        SystemData = systemData;
        ComponentDatas = componentDatas;
    }
}
