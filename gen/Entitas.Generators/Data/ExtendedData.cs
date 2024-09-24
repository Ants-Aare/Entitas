using System.Collections.Immutable;
using System.Linq;
using Entitas.Generators.Common;

namespace Entitas.Generators.Data;

//I thought this would be neat, but I need to override the equality operators everywhere so that I can call SequenceEqual() instead of Equals()
public record struct ExtendedComponentData(ComponentData ComponentData, ImmutableArray<ContextData> ContextDatas)
{
    public readonly bool Equals(ExtendedComponentData other)
    {
        return ComponentData.Equals(other.ComponentData)
               && ContextDatas.SequenceEqual(other.ContextDatas);
    }

    public readonly override int GetHashCode()
    {
        unchecked
        {
            return (ComponentData.GetHashCode() * 397) ^ ContextDatas.SequenceGetHashCode();
        }
    }
}

public record struct ExtendedComponentDataWithSystemsAndGroups(ComponentData ComponentData, ContextData ContextData, ImmutableArray<SystemData> SystemDatas, ImmutableArray<GroupData> GroupDatas)
{
    public readonly bool Equals(ExtendedComponentDataWithSystemsAndGroups other)
    {
        return ComponentData.Equals(other.ComponentData)
               && ContextData.Equals(other.ContextData)
               && SystemDatas.SequenceEqual(other.SystemDatas)
               && GroupDatas.SequenceEqual(other.GroupDatas);
    }

    public readonly override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ComponentData.GetHashCode();
            hashCode = (hashCode * 397) ^ ContextData.GetHashCode();
            hashCode = (hashCode * 397) ^ SystemDatas.SequenceGetHashCode();
            hashCode = (hashCode * 397) ^ GroupDatas.SequenceGetHashCode();
            return hashCode;
        }
    }
}

public record struct ExtendedGroupData(GroupData GroupData, ImmutableArray<ContextData> ContextDatas)
{
    public readonly bool Equals(ExtendedGroupData other)
    {
        return GroupData.Equals(other.GroupData)
               && ContextDatas.SequenceEqual(other.ContextDatas);
    }

    public readonly override int GetHashCode()
    {
        unchecked
        {
            return (GroupData.GetHashCode() * 397) ^ ContextDatas.SequenceGetHashCode();
        }
    }
}

public record struct ExtendedContextData(ContextData ContextData, ImmutableArray<ComponentData> ComponentDatas, ImmutableArray<SystemData> SystemDatas, ImmutableArray<GroupData> GroupDatas)
{
    public readonly bool Equals(ExtendedContextData other)
    {
        return ContextData.Equals(other.ContextData)
            && ComponentDatas.SequenceEqual(other.ComponentDatas)
            && SystemDatas.SequenceEqual(other.SystemDatas)
            && GroupDatas.SequenceEqual(other.GroupDatas);
    }

    public readonly override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ContextData.GetHashCode();
            hashCode = (hashCode * 397) ^ ComponentDatas.SequenceGetHashCode();
            hashCode = (hashCode * 397) ^ SystemDatas.SequenceGetHashCode();
            hashCode = (hashCode * 397) ^ GroupDatas.SequenceGetHashCode();
            return hashCode;
        }
    }
}

public record struct ExtendedArchetypeData(ArchetypeData ArchetypeData, ImmutableArray<ContextData> ContextDatas, ImmutableArray<ComponentData> ComponentDatas)
{
    public readonly bool Equals(ExtendedArchetypeData other)
    {
        return ArchetypeData.Equals(other.ArchetypeData)
            && ContextDatas.SequenceEqual(other.ContextDatas)
            && ComponentDatas.SequenceEqual(other.ComponentDatas);
    }

    public readonly override int GetHashCode()
    {
        unchecked
        {
            var hashCode = ArchetypeData.GetHashCode();
            hashCode = (hashCode * 397) ^ ContextDatas.SequenceGetHashCode();
            hashCode = (hashCode * 397) ^ ComponentDatas.SequenceGetHashCode();
            return hashCode;
        }
    }
}
