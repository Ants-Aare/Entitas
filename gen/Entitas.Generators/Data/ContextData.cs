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

public struct ContextData : IClassDeclarationResolver, IAttributeResolver, IFinalisable<ContextData>, IEquatable<ContextData>
{
    public string? Namespace { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }

    public string FullPrefix { get; private set; }
    public string Prefix { get; private set; }

    public ImmutableArray<string> Components = ImmutableArray<string>.Empty;
    public ImmutableArray<string> Systems = ImmutableArray<string>.Empty;
    public ImmutableArray<string> Features = ImmutableArray<string>.Empty;

    readonly HashSet<string> _components = new();
    readonly HashSet<string> _systems = new();
    readonly HashSet<string> _features = new();

    public ContextData()
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
                   Name: IdentifierNameSyntax { Identifier.Text: ContextName or ContextAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: ContextName or ContextAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        Namespace = symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();

        FullName = symbol.ToDisplayString();
        Name = symbol.Name;

        FullPrefix = FullName.RemoveSuffix("Context");
        Prefix = Name.RemoveSuffix("Context");
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: WithComponentsAttributeName } => TryResolveComponentsAttribute(attributeData),
            { AttributeClass.Name: WithSystemsAttributeName } => TryResolveSystemsAttribute(attributeData),
            { AttributeClass.Name: WithFeaturesAttributeName } => TryResolveFeaturesAttribute(attributeData),
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

    bool TryResolveFeaturesAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _features.UnionWith(values.Where(x=> x.Value is string).Select(x=> (string) x.Value!));
        return true;
    }

    public ContextData Finalise()
    {
        Components = _components.ToImmutableArray();
        Systems = _systems.ToImmutableArray();
        Features = _features.ToImmutableArray();
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
            stringBuilder.AppendLine(e.ToString());
        }

        return stringBuilder.ToString();
    }

    public bool Equals(ContextData other)
    {
        return Components.Equals(other.Components) && Systems.Equals(other.Systems) && Features.Equals(other.Features) && FullName == other.FullName;
    }

    public override bool Equals(object? obj)
    {
        return obj is ContextData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Components.GetHashCode();
            hashCode = (hashCode * 397) ^ Systems.GetHashCode();
            hashCode = (hashCode * 397) ^ Features.GetHashCode();
            hashCode = (hashCode * 397) ^ FullName.GetHashCode();
            return hashCode;
        }
    }
}
