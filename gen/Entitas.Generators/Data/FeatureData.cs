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

public struct FeatureData : IClassDeclarationResolver, IAttributeResolver, IFinalisable<FeatureData>, IEquatable<FeatureData>
{
    public string? Namespace { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }
    public string FullPrefix { get; private set; }
    public string Prefix { get; private set; }
    public ImmutableArray<string> ManuallyAddedContexts { get; private set; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> Components = ImmutableArray<string>.Empty;
    public ImmutableArray<string> Systems = ImmutableArray<string>.Empty;

    readonly HashSet<string> _components = new();
    readonly HashSet<string> _systems = new();

    public FeatureData()
    {
        Namespace = null;
        FullName = null!;
        Name = null!;
        FullPrefix = null!;
        Prefix = null!;
    }

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken ct)
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
        Namespace = symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();

        FullName = symbol.ToDisplayString();
        Name = symbol.Name;

        FullPrefix = FullName.RemoveSuffix("Feature");
        Prefix = Name.RemoveSuffix("Feature");
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
        _components.UnionWith(values.Where(x=> x.Value is string).Select(x=> (string) x.Value!));
        return true;
    }

    bool TryResolveSystemsAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _systems.UnionWith(values.Where(x=> x.Value is string).Select(x=> (string) x.Value!));
        return true;
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var typedConstants = attributeData.ConstructorArguments[0].Values;
        ManuallyAddedContexts = typedConstants.Select(x => (string)x.Value!).ToImmutableArray();
        return true;
    }

    public FeatureData Finalise()
    {
        Components = _components.ToImmutableArray();
        Systems = _systems.ToImmutableArray();
        return this;
    }

    public static string ToEntityInterfaceString(string fullName)
    {
        if (!fullName.Contains('.'))
            return $"I{fullName}Context";
        var lastDotIndex = fullName.LastIndexOf('.') + 1;
        return $"{fullName.Substring(0, lastDotIndex)}I{fullName.Substring(lastDotIndex)}Entity";
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

            if (ManuallyAddedContexts.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(ManuallyAddedContexts)}:");
                foreach (var contexts in ManuallyAddedContexts)
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

    public bool Equals(FeatureData other)
    {
        return Components.Equals(other.Components) && Systems.Equals(other.Systems) && FullName == other.FullName && ManuallyAddedContexts.Equals(other.ManuallyAddedContexts);
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
            hashCode = (hashCode * 397) ^ ManuallyAddedContexts.GetHashCode();
            return hashCode;
        }
    }
}
