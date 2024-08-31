namespace Entitas.Generators.Data;

public record struct ComponentEventData(EventTarget EventTarget, EventType EventType, int Order);
