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

public struct ComponentData() : IClassDeclarationResolver, IAttributeResolver, IFieldResolver, IMethodResolver, IFinalisable<ComponentData>, IComparable<ComponentData>, IComparable, IEquatable<ComponentData>
{
    sealed class TypeAndFieldsEqualityComparer : IEqualityComparer<ComponentData>
    {
        public bool Equals(ComponentData x, ComponentData y)
        {
            return x.TypeData.Equals(y.TypeData) && x.Fields.Equals(y.Fields);
        }

        public int GetHashCode(ComponentData obj)
        {
            unchecked
            {
                return (obj.TypeData.GetHashCode() * 397) ^ obj.Fields.GetHashCode();
            }
        }
    }
    public static IEqualityComparer<ComponentData> TypeAndFieldsComparer { get; } = new TypeAndFieldsEqualityComparer();

    public TypeData TypeData { get; private set; } = default;
    public ImmutableArray<FieldData> Fields { get; private set; } = ImmutableArray<FieldData>.Empty;
    public ImmutableArray<EventData> Events = ImmutableArray<EventData>.Empty;
    public ImmutableArray<TypeData> Contexts = ImmutableArray<TypeData>.Empty;

    public bool IsUnique { get; private set; } = false;
    public EntityIndexType IndexType { get; private set; } = EntityIndexType.None;
    public int IndexMaxSize { get; private set; } = 1000;
    public string? GetIndexMethod { get; private set; } = null;
    public bool IsCleanup { get; private set; } = false;
    public CleanupMode CleanupMode { get; private set; } = CleanupMode.RemoveComponent;
    public Execution CleanupExecution { get; private set; } = Execution.PostUpdate;
    public int CleanupOrder { get; private set; } = 0;

    public string? Namespace => TypeData.Namespace;
    public string FullName => TypeData.FullName;
    public string Name => TypeData.Name;
    public string FullPrefix => TypeData.FullPrefix!;
    public string Prefix => TypeData.Prefix!;
    public bool HasEvents => Events.Length != 0;

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken _)
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
        TypeData = TypeData.Create(symbol, ComponentName);
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: UniqueAttributeName } => TryResolveUniqueAttribute(attributeData),
            { AttributeClass.Name: ComponentAttributeName } => TryResolveComponentAttribute(attributeData),
            // { AttributeClass.Name: EventAttributeName } => TryResolveEventAttribute(attributeData),
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
        Contexts = typedConstants.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ContextName)).ToImmutableArray();
        return true;
    }

    bool TryResolveCleanupAttribute(AttributeData attributeData)
    {
        IsCleanup = true;
        CleanupMode = (CleanupMode)(attributeData.ConstructorArguments[0].Value ?? CleanupMode == CleanupMode.RemoveComponent);
        CleanupExecution = (Execution)(attributeData.ConstructorArguments[1].Value ?? Execution.PostUpdate);
        CleanupOrder = (int)(attributeData.ConstructorArguments[2].Value ?? 0);

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

    public ComponentData? Finalise()
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

            if (Contexts.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(Contexts)}:");
                foreach (var contexts in Contexts)
                {
                    stringBuilder.AppendLine($"      {contexts}");
                }
            }

            if (GetIndexMethod != null)
                stringBuilder.AppendLine(GetIndexMethod);
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        return stringBuilder.ToString();
    }

    public bool Equals(ComponentData other)
    {
        return TypeData.Equals(other.TypeData)
               && IsUnique == other.IsUnique
               && IndexType == other.IndexType
               && IndexMaxSize == other.IndexMaxSize
               && IsCleanup == other.IsCleanup
               && CleanupMode == other.CleanupMode
               && CleanupExecution == other.CleanupExecution
               && CleanupOrder == other.CleanupOrder
               && Contexts.SequenceEqual(other.Contexts)
               && Fields.SequenceEqual(other.Fields);
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentData other && Equals(other);
    }

    public override int GetHashCode() => TypeData.GetHashCode();

    public int CompareTo(ComponentData other)
    {
        var isUniqueComparison = IsUnique.CompareTo(other.IsUnique);
        return isUniqueComparison != 0 ? isUniqueComparison : string.Compare(Prefix, other.Prefix, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is ComponentData other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(ComponentData)}");
    }
}

public enum CleanupMode
{
    RemoveComponent = 0,
    DestroyEntity = 1,
}

// public enum EventTarget
// {
//     Any = 0,
//     Self = 1,
// }

// public enum EventType
// {
//     Added = 0,
//     Removed = 1,
//     AddedOrRemoved = 2,
// }

public enum EntityIndexType
{
    Array = 0,
    Dictionary = 1,
    None = -1000,
}
