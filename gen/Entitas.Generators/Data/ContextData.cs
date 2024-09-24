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

public struct ContextData() : IClassDeclarationResolver, IAttributeResolver, IFinalisable<ContextData>, IEquatable<ContextData>, IComparable<ContextData>, IComparable
{
    public TypeData TypeData { get; private set; } = default;
    public ImmutableArray<TypeData> Components { get; set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> Systems { get; set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> Features { get; set; } = ImmutableArray<TypeData>.Empty;

    public bool IsUnique { get; private set; } = false;

    readonly HashSet<TypeData> _components = new();
    readonly HashSet<TypeData> _systems = new();
    readonly HashSet<TypeData> _features = new();

    public string? Namespace => TypeData.Namespace;
    public string FullName => TypeData.FullName;
    public string Name => TypeData.Name;
    public string FullPrefix => TypeData.FullPrefix!;
    public string Prefix => TypeData.Prefix!;

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: ContextName or ContextAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: ContextName or ContextAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        TypeData = TypeData.Create(symbol, ContextName);
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: ContextAttributeName } => TryResolveContextAttribute(attributeData),
            { AttributeClass.Name: UniqueAttributeName } => TryResolveUniqueAttribute(attributeData),
            { AttributeClass.Name: WithComponentsAttributeName } => TryResolveComponentsAttribute(attributeData),
            { AttributeClass.Name: WithSystemsAttributeName } => TryResolveSystemsAttribute(attributeData),
            { AttributeClass.Name: WithFeaturesAttributeName } => TryResolveFeaturesAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveContextAttribute(AttributeData attributeData)
    {
        IsUnique = (bool?)attributeData.ConstructorArguments[0].Value ?? false;
        return true;
    }

    bool TryResolveUniqueAttribute(AttributeData _)
    {
        IsUnique = true;
        return true;
    }

    bool TryResolveComponentsAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _components.UnionWith(values.Where(x => x.Value is INamedTypeSymbol).Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName)));
        return true;
    }

    bool TryResolveSystemsAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _systems.UnionWith(values.Where(x => x.Value is INamedTypeSymbol).Select(x => TypeData.Create((INamedTypeSymbol)x.Value!)));
        return true;
    }

    bool TryResolveFeaturesAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _features.UnionWith(values.Where(x => x.Value is INamedTypeSymbol).Select(x => TypeData.Create((INamedTypeSymbol)x.Value!)));
        return true;
    }

    public ContextData? Finalise()
    {
        Components = _components.ToImmutableArray();
        Systems = _systems.ToImmutableArray();
        Features = _features.ToImmutableArray();
        _components.Clear();
        _systems.Clear();
        _features.Clear();
        return this;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("ContextData:\n");
        try
        {
            stringBuilder
                .AppendLine($"   {nameof(Namespace)}: {Namespace}")
                .AppendLine($"   {nameof(FullName)}: {FullName}")
                .AppendLine($"   {nameof(Name)}: {Name}")
                .AppendLine($"   {nameof(FullPrefix)}: {FullPrefix}")
                .AppendLine($"   {nameof(Prefix)}: {Prefix}");

            if (Components.Length != 0)
            {
                stringBuilder.AppendLine($"   {nameof(Components)}:");
                foreach (var component in Components)
                {
                    stringBuilder.AppendLine($"      {component}");
                }
            }
            else
                stringBuilder.AppendLine("This Context has no Components declared on it.");

            if (Systems.Length != 0)
            {
                stringBuilder.AppendLine($"   {nameof(Systems)}:");
                foreach (var system in Systems)
                {
                    stringBuilder.AppendLine($"      {system}");
                }
            }
            else
                stringBuilder.AppendLine("This Context has no Systems declared on it.");

            if (Features.Length != 0)
            {
                stringBuilder.AppendLine($"   {nameof(Features)}:");
                foreach (var feature in Features)
                {
                    stringBuilder.AppendLine($"      {feature}");
                }
            }
            else
                stringBuilder.AppendLine("This Context has no Features declared on it.");
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        return stringBuilder.ToString();
    }

    public bool Equals(ContextData other)
    {
        return TypeData == other.TypeData
               && Components.SequenceEqual(other.Components)
               && Systems.SequenceEqual(other.Systems)
               && Features.SequenceEqual(other.Features);
    }

    public override bool Equals(object? obj)
    {
        return obj is ContextData other && Equals(other);
    }

    public override int GetHashCode() => TypeData.GetHashCode();

    public int CompareTo(ContextData other)
    {
        return string.Compare(Prefix, other.Prefix, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is ContextData other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(ContextData)}");
    }
}
