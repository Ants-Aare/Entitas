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

public struct FeatureData() : IClassDeclarationResolver, IAttributeResolver, IFinalisable<FeatureData>, IEquatable<FeatureData>
{
    public TypeData TypeData { get; private set; } = default;
    public ImmutableArray<TypeData> Contexts { get; private set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> Components = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> Systems = ImmutableArray<TypeData>.Empty;

    readonly HashSet<TypeData> _components = new();
    readonly HashSet<TypeData> _systems = new();
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
                   Name: IdentifierNameSyntax { Identifier.Text: FeatureName or FeatureAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: FeatureName or FeatureAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        TypeData = TypeData.Create(symbol, FeatureName);
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: WithComponentsAttributeName } => TryResolveComponentsAttribute(attributeData),
            { AttributeClass.Name: WithSystemsAttributeName } => TryResolveSystemsAttribute(attributeData),
            { AttributeClass.Name: AddToContextAttributeName } => TryResolveAddToContextAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveComponentsAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _components.UnionWith(values.Where(x=> x.Value is INamedTypeSymbol).Select(x=> TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName)));
        return true;
    }

    bool TryResolveSystemsAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _systems.UnionWith(values.Where(x=> x.Value is INamedTypeSymbol).Select(x=> TypeData.Create((INamedTypeSymbol)x.Value!)));
        return true;
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var typedConstants = attributeData.ConstructorArguments[0].Values;
        Contexts = typedConstants.Where(x=> x.Value is INamedTypeSymbol).Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ContextName)).ToImmutableArray();
        return true;
    }

    public FeatureData? Finalise()
    {
        if (_components.Count == 0 && _systems.Count == 0)
            return null;

        Components = _components.ToImmutableArray();
        Systems = _systems.ToImmutableArray();
        _components.Clear();
        _systems.Clear();
        return this;
    }

    public static string ToContextInterfaceString(string fullName)
    {
        if (!fullName.Contains('.'))
            return $"I{fullName}Context";
        var lastDotIndex = fullName.LastIndexOf('.') + 1;
        return $"{fullName.Substring(0, lastDotIndex)}I{fullName.Substring(lastDotIndex)}Context";
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("FeatureData:\n");
        try
        {
            stringBuilder
                .AppendLine($"   {nameof(Namespace)}: {Namespace}")
                .AppendLine($"   {nameof(FullName)}: {FullName}")
                .AppendLine($"   {nameof(Name)}: {Name}")
                .AppendLine($"   {nameof(FullPrefix)}: {FullPrefix}")
                .AppendLine($"   {nameof(Prefix)}: {Prefix}");

            stringBuilder.AppendLine($"   {nameof(Components)}:");
            if (Components.Length != 0)
            {
                foreach (var component in Components)
                {
                    stringBuilder.AppendLine($"      {component}");
                }
            }
            else
                stringBuilder.AppendLine("This Feature has no Components declared on it.");

            stringBuilder.AppendLine($"   {nameof(Systems)}:");
            if (Systems.Length != 0)
            {
                foreach (var system in Systems)
                {
                    stringBuilder.AppendLine($"      {system}");
                }
            }
            else
                stringBuilder.AppendLine("This Feature has no Systems declared on it.");

            if (Contexts.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(Contexts)}:");
                foreach (var contexts in Contexts)
                {
                    stringBuilder.AppendLine($"      {contexts}");
                }
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        return stringBuilder.ToString();
    }

    public bool Equals(FeatureData other)
    {
        return Components.Equals(other.Components) && Systems.Equals(other.Systems) && FullName == other.FullName && Contexts.Equals(other.Contexts);
    }

    public override bool Equals(object? obj)
    {
        return obj is FeatureData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Components.GetHashCode();
            hashCode = (hashCode * 397) ^ Systems.GetHashCode();
            hashCode = (hashCode * 397) ^ FullName.GetHashCode();
            hashCode = (hashCode * 397) ^ Contexts.GetHashCode();
            return hashCode;
        }
    }
}
