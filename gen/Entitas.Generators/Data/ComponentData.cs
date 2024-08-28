using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.StringConstants;

namespace Entitas.Generators.Data;

public struct ComponentData : IClassDeclarationResolver, IAttributeResolver, IFieldResolver, IMethodResolver, IFinalisable<ComponentData>, IEquatable<ComponentData>
{
    public string? Namespace { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }
    public string FullPrefix { get; private set; }
    public string Prefix { get; private set; }

    public ImmutableArray<FieldData> Fields { get; private set; } = ImmutableArray<FieldData>.Empty;
    public ImmutableArray<ComponentEventData> Events { get; private set; } = ImmutableArray<ComponentEventData>.Empty;
    public ImmutableArray<string> ComponentAddedContexts { get; private set; } = ImmutableArray<string>.Empty;

    public bool IsUnique { get; private set; }
    public EntityIndexType IndexType { get; private set; } = EntityIndexType.None;
    public int IndexMaxSize { get; private set; } = 1000;
    public string? GetIndexMethod { get; private set; }
    public CleanupMode? CleanupMode { get; private set; }

    public ComponentData()
    {
        Namespace = null;
        FullName = null!;
        Name = null!;
        FullPrefix = null!;
        Prefix = null!;
        IsUnique = false;
        CleanupMode = null;
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

        FullPrefix = FullName.RemoveSuffix("Component");
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
            { AttributeClass.Name: IndexedAttributeName } => TryResolveIndexedAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveIndexedAttribute(AttributeData attributeData)
    {
        IndexType = (EntityIndexType)(attributeData.ConstructorArguments[0].Value ?? EntityIndexType.Dictionary);
        IndexMaxSize = (int)(attributeData.ConstructorArguments[1].Value ?? 1000);
        return true;
    }

    bool TryResolveComponentAttribute(AttributeData attributeData)
    {
        IsUnique = (bool?)attributeData.ConstructorArguments[0].Value ?? false;
        return true;
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var typedConstants = attributeData.ConstructorArguments[0].Values;
        ComponentAddedContexts = typedConstants.Select(x => (string)x.Value!).ToImmutableArray();
        return true;
    }

    bool TryResolveCleanupAttribute(AttributeData attributeData)
    {
        CleanupMode = (CleanupMode)(attributeData.ConstructorArguments[0].Value ?? CleanupMode == Data.CleanupMode.RemoveComponent);
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

        if (!Events.Contains(eventData))
            Events = Events.Add(eventData);
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
        // fieldData.ResolveAttributes(fieldSymbol);

        if (!Fields.Contains(fieldData))
            Fields = Fields.Add(fieldData);

        return true;
    }

    public bool TryResolveMethod(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Name != "GetIndex")
            return true;

        var syntaxReferences = methodSymbol.DeclaringSyntaxReferences;
        GetIndexMethod = syntaxReferences.First().GetSyntax().NormalizeWhitespace(string.Empty).GetText().ToString();
        return true;
    }

    public ComponentData Finalise()
    {
        if (IsUnique)
            IndexType = EntityIndexType.None;
        if (IndexType != EntityIndexType.None && Fields.Length == 0)
            IndexType = EntityIndexType.None;
        return this;
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

            if (GetIndexMethod != null)
                stringBuilder.AppendLine(GetIndexMethod);
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        return stringBuilder.ToString();
    }

    public bool Equals(ComponentData other)
    {
        return FullName == other.FullName && Fields.Equals(other.Fields) && Events.Equals(other.Events) && ComponentAddedContexts.Equals(other.ComponentAddedContexts) && IsUnique == other.IsUnique && CleanupMode == other.CleanupMode;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = FullName.GetHashCode();
            hashCode = (hashCode * 397) ^ Fields.GetHashCode();
            hashCode = (hashCode * 397) ^ Events.GetHashCode();
            hashCode = (hashCode * 397) ^ ComponentAddedContexts.GetHashCode();
            hashCode = (hashCode * 397) ^ IsUnique.GetHashCode();
            hashCode = (hashCode * 397) ^ CleanupMode.GetHashCode();
            return hashCode;
        }
    }
}

// public readonly struct MethodData
// {
//     public readonly string Me
// }

public enum CleanupMode
{
    RemoveComponent = 0,
    DestroyEntity = 1,
}

public enum EventTarget
{
    Any = 0,
    Self = 1,
}

public enum EventType
{
    Added = 0,
    Removed = 1,
    AddedOrRemoved = 2,
}

public enum EntityIndexType
{
    Array = 0,
    Dictionary = 1,
    None = -1000,
}
