using System;

namespace Entitas.Generators.Data;

public struct ComponentEventData : IEquatable<ComponentEventData>
{
    public bool Equals(ComponentEventData other)
    {
        return EventTarget == other.EventTarget && EventType == other.EventType && Order == other.Order;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentEventData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)EventTarget;
            hashCode = (hashCode * 397) ^ (int)EventType;
            hashCode = (hashCode * 397) ^ Order;
            return hashCode;
        }
    }

    public EventTarget EventTarget;
    public EventType EventType;
    public int Order;

    public override string ToString()
    {
        return $"EventData: Order.{Order} Target.{EventTarget} EventType.{EventType}";
    }
}
