using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.Utility.StringConstants;

namespace Entitas.Generators.Data;

public struct ListenerData() : IClassDeclarationResolver, IAttributeResolver, IFinalisable<ListenerData>, IComparable<ListenerData>, IEquatable<ListenerData>, IComparable
{
    public TypeData TypeData { get; private set; } = default;
    public ImmutableArray<EventData> Events = ImmutableArray<EventData>.Empty;
    // public ImmutableArray<TypeData> EntityIs { get; private set; } = ImmutableArray<TypeData>.Empty;
    readonly List<EventData> _events = new();

    public string? Namespace => TypeData.Namespace;
    public string FullName => TypeData.FullName;
    public string Name => TypeData.Name;

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: ListenToName or ListenToAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: ListenToName or ListenToAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        TypeData = TypeData.Create(symbol);
        // MakeMethodsVirtual = symbol.IsSealed;
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: ListenToAttributeName } => TryResolveListenToAttribute(attributeData),
            // { AttributeClass.Name: EntityIsAttributeName } => TryResolveEntityIsAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveListenToAttribute(AttributeData attributeData)
    {
        var typeData = TypeData.Create((INamedTypeSymbol)attributeData.ConstructorArguments[0].Value!, ComponentName);
        var componentEvent = (ComponentEvent)(attributeData.ConstructorArguments[1].Value ?? ComponentEvent.Added);
        var listenTarget = (ListenTarget)(attributeData.ConstructorArguments[2].Value ?? ListenTarget.Entity);
        var allowMultipleListeners = (bool)(attributeData.ConstructorArguments[3].Value ?? false);
        var eventExecution = (EventExecution)(attributeData.ConstructorArguments[4].Value ?? EventExecution.Instant);
        var order = (int)(attributeData.ConstructorArguments[5].Value ?? 0);

        var listenerEventData = new EventData(typeData, componentEvent, listenTarget, allowMultipleListeners, eventExecution, order);
        _events.Add(listenerEventData);
        return true;
    }

    // bool TryResolveEntityIsAttribute(AttributeData attributeData)
    // {
    //     EntityIs = attributeData.ConstructorArguments[0].Values
    //         .Where(x => x.Value is INamedTypeSymbol)
    //         .Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName))
    //         .ToImmutableArray();
    //     return true;
    // }

    public ListenerData? Finalise()
    {
        Events = _events.ToImmutableArray();
        _events.Clear();
        return this;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("ListenerData:\n");
        try
        {
            stringBuilder.AppendLine($"   {nameof(Namespace)}: {Namespace}")
                .AppendLine($"   {nameof(FullName)}: {FullName}")
                .AppendLine($"   {nameof(Name)}: {Name}");

            if (Events.Length > 0)
            {
                stringBuilder.AppendLine($"\t{nameof(Events)}:");
                foreach (var eventData in Events)
                {
                    stringBuilder.AppendLine($"\t\t{eventData.Component.FullName}: {nameof(ComponentEvent)}.{eventData.ComponentEvent}, {nameof(ListenTarget)}.{eventData.ListenTarget}, AllowMultipleListeners:{eventData.AllowMultipleListeners}, {nameof(EventExecution)}.{eventData.Execution}, Order:{eventData.Order}");
                }
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        return stringBuilder.ToString();
    }

    public bool Equals(ListenerData other)
    {
        return TypeData.Equals(other.TypeData)
               && Events.SequenceEqual(other.Events);
    }

    public override bool Equals(object? obj)
    {
        return obj is ListenerData other && Equals(other);
    }

    public override int GetHashCode() => TypeData.GetHashCode();

    public int CompareTo(ListenerData other)
    {
        return TypeData.CompareTo(other.TypeData);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is ListenerData other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(ListenerData)}");
    }
}

public record struct EventData(TypeData Component, ComponentEvent ComponentEvent = ComponentEvent.Added, ListenTarget ListenTarget = ListenTarget.Entity, bool AllowMultipleListeners = false, EventExecution Execution = EventExecution.Instant, int Order = 0)
{
    public bool HasAnyUpdateExecution
        => Execution.HasFlagFast(EventExecution.PreUpdate)
           || Execution.HasFlagFast(EventExecution.PostUpdate)
           || Execution.HasFlagFast(EventExecution.PreLateUpdate)
           || Execution.HasFlagFast(EventExecution.PostLateUpdate)
           || Execution.HasFlagFast(EventExecution.PreFixedUpdate)
           || Execution.HasFlagFast(EventExecution.PostFixedUpdate);
}

public enum ListenTarget
{
    Entity = 0,
    Context = 1,
}

[Flags]
public enum ComponentEvent
{
    Added = 1,
    Changed = 2,
    Set = 4,
    Removed = 8,
    AddedOrRemoved = Added | Removed,
    Updated = Added | Set | Changed,
    AllEvents = Added | Changed | Set | Removed,
}

public static class ComponentEventExtensions
{
    public static bool HasFlagFast(this ComponentEvent value, ComponentEvent flag)
    {
        return (value & flag) != 0;
    }
}

[Flags]
public enum EventExecution
{
    PreUpdate = 1 << 0,
    PostUpdate = 1 << 1,

    PreLateUpdate = 1 << 2,
    PostLateUpdate = 1 << 3,

    PreFixedUpdate = 1 << 4,
    PostFixedUpdate = 1 << 5,

    Instant = 1 << 7,
}

public static class EventExecutionExtensions
{
    public static bool HasFlagFast(this EventExecution value, EventExecution flag)
    {
        return (value & flag) != 0;
    }
}
