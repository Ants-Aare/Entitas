using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.StringConstants;

namespace Entitas.Generators.Data;

public struct ComponentData : IClassDeclarationResolver, IAttributeResolver, IFieldResolver, IFinalizable, IEquatable<ComponentData>
{
    public string? Namespace { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }
    public string FullPrefix { get; private set; }
    public string Prefix { get; private set; }

    public ImmutableArray<FieldData> Fields { get; private set; }
    public ImmutableArray<ComponentEventData> Events { get; private set; }
    public ImmutableArray<int> ComponentAddedContexts { get; private set; }

    List<FieldData> _fields = new();
    HashSet<ComponentEventData> _events = new();
    HashSet<int> _componentAddedContexts = new();
    public bool IsUnique { get; private set; }
    public CleanupMode CleanupMode { get; private set; }

    public ComponentData()
    {
        Namespace = null;
        FullName = null!;
        Name = null!;
        FullPrefix = null!;
        Prefix = null!;
        Fields = ImmutableArray<FieldData>.Empty;
        Events = ImmutableArray<ComponentEventData>.Empty;
        ComponentAddedContexts = ImmutableArray<int>.Empty;
        IsUnique = false;
        CleanupMode = CleanupMode.None;
    }

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken ct)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: ComponentName or ComponentAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: ComponentName or ComponentAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        Namespace = symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();

        FullName = symbol.ToDisplayString();
        Name = symbol.Name;

        FullPrefix = FullName.Replace(".", string.Empty).RemoveSuffix("Component");
        Prefix = Name.RemoveSuffix("Component");
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: UniqueAttributeName } => TryResolveUniqueAttribute(attributeData),
            { AttributeClass.Name: ComponentAttributeName } => TryResolveComponentAttribute(attributeData),
            { AttributeClass.Name: EventAttributeName } => TryResolveEventAttribute(attributeData),
            { AttributeClass.Name: CleanupAttributeName } => TryResolveCleanupAttribute(attributeData),
            { AttributeClass.Name: AddToContextAttributeName } => TryResolveAddToContextAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveComponentAttribute(AttributeData attributeData)
    {
        IsUnique = (bool?)attributeData.ConstructorArguments.FirstOrDefault().Value ?? false;
        return true;
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var contexts = attributeData.ConstructorArguments.FirstOrDefault().Values;
        foreach (var context in contexts)
        {
            if (context.Value == null)
                continue;
            _componentAddedContexts.Add((int)context.Value);
        }

        return true;
    }

    bool TryResolveCleanupAttribute(AttributeData attributeData)
    {
        CleanupMode = (CleanupMode)(attributeData.ConstructorArguments.FirstOrDefault().Value ?? CleanupMode.None);
        return true;
    }

    bool TryResolveEventAttribute(AttributeData attributeData)
    {
        var eventData = new ComponentEventData
        {
            EventTarget = (EventTarget)(attributeData.ConstructorArguments[0].Value ?? EventTarget.Any),
            EventType = (EventType)(attributeData.ConstructorArguments[1].Value ?? EventType.Added),
            Order = (int?)attributeData.ConstructorArguments[2].Value ?? 0
        };

        _events.Add(eventData);
        return true;
    }

    bool TryResolveUniqueAttribute(AttributeData _)
    {
        IsUnique = true;
        return true;
    }

    public bool TryResolveField(IFieldSymbol fieldSymbol)
    {
        if (fieldSymbol.DeclaredAccessibility != Accessibility.Public)
            return true;

        var fieldData = new FieldData();

        fieldData.TryResolveField(fieldSymbol);
        fieldData.ResolveAttributes(fieldSymbol);

        _fields.Add(fieldData);

        return true;
    }

    public void FinalizeStruct()
    {
        Fields = _fields.ToImmutableArray();
        ComponentAddedContexts = _componentAddedContexts.ToImmutableArray();
        Events = _events.ToImmutableArray();

        _fields.Clear();
        _componentAddedContexts.Clear();
        _events.Clear();
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("ComponentData:\n");
        try
        {
            stringBuilder.AppendLine($"   {nameof(Namespace)}: {Namespace}")
                .AppendLine($"   {nameof(FullName)}: {FullName}")
                .AppendLine($"   {nameof(Name)}: {Name}")
                .AppendLine($"   {nameof(FullPrefix)}: {FullPrefix}")
                .AppendLine($"   {nameof(Prefix)}: {Prefix}")
                .AppendLine($"   {nameof(IsUnique)}: {IsUnique}")
                .AppendLine($"   {nameof(CleanupMode)}: {CleanupMode}");


            stringBuilder.AppendLine($"   {nameof(Fields)}:");

            if (Fields.Length > 0)
            {
                foreach (var fieldData in Fields)
                {
                    stringBuilder.AppendLine($"      {fieldData.ToString()}");
                }
            }
            else
            {
                stringBuilder.AppendLine($"      This Component doesn't have any fields.");
            }

            if (Events.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(Events)}:");
                foreach (var eventData in Events)
                {
                    stringBuilder.AppendLine($"      {eventData.ToString()}");
                }
            }

            if (ComponentAddedContexts.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(ComponentAddedContexts)}:");
                foreach (var contexts in ComponentAddedContexts)
                {
                    stringBuilder.AppendLine($"      {contexts}");
                }
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        return stringBuilder.ToString();
    }

    public bool Equals(ComponentData other)
    {
        return Namespace == other.Namespace && FullName == other.FullName && Name == other.Name && FullPrefix == other.FullPrefix && Prefix == other.Prefix && Fields.Equals(other.Fields) && Events.Equals(other.Events) && ComponentAddedContexts.Equals(other.ComponentAddedContexts) && IsUnique == other.IsUnique && CleanupMode == other.CleanupMode;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (Namespace != null ? Namespace.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ FullName.GetHashCode();
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            hashCode = (hashCode * 397) ^ FullPrefix.GetHashCode();
            hashCode = (hashCode * 397) ^ Prefix.GetHashCode();
            hashCode = (hashCode * 397) ^ Fields.GetHashCode();
            hashCode = (hashCode * 397) ^ Events.GetHashCode();
            hashCode = (hashCode * 397) ^ ComponentAddedContexts.GetHashCode();
            hashCode = (hashCode * 397) ^ IsUnique.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)CleanupMode;
            return hashCode;
        }
    }
}

public enum CleanupMode
{
    RemoveComponent,
    DestroyEntity,
    None,
}

public enum EventTarget
{
    Any,
    Self
}

public enum EventType
{
    Added,
    Removed
}
