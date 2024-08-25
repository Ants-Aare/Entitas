using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.StringConstants;

namespace Entitas.Generators.Data;

public struct ContextData : IClassDeclarationResolver, IAttributeResolver
{
    public string? Namespace { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }

    public string FullPrefix { get; private set; }
    public string Prefix { get; private set; }

    public readonly HashSet<int> Components = new();
    public readonly HashSet<int> Systems = new();
    public int Index;

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

        FullPrefix = FullName.Replace(".", string.Empty).RemoveSuffix("Context");
        Prefix = Name.RemoveSuffix("Context");
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: WithComponentsAttributeName } => TryResolveComponentsAttribute(attributeData),
            { AttributeClass.Name: WithSystemsAttributeName } => TryResolveSystemsAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveComponentsAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        foreach (var value in values)
        {
            if (value.Value is not null)
                Components.Add((int)value.Value);
        }

        return true;
    }

    bool TryResolveSystemsAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        foreach (var value in values)
        {
            if (value.Value is not null)
                Systems.Add((int)value.Value);
        }

        return true;
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

            stringBuilder.AppendLine($"   {nameof(Components)}:");
            if (Components.Count != 0)
            {
                foreach (var component in Components)
                {
                    stringBuilder.AppendLine($"      {component}");
                }
            }
            else
                stringBuilder.AppendLine("This Context has no Components declared on it.");

            stringBuilder.AppendLine($"   {nameof(Systems)}:");
            if (Systems.Count != 0)
            {
                foreach (var system in Systems)
                {
                    stringBuilder.AppendLine($"      {system}");
                }
            }
            else
                stringBuilder.AppendLine("This Context has no Systems declared on it.");
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        return stringBuilder.ToString();
    }

    public static ContextData SetIndex(ContextData data, int i)
    {
        data.Index = i;
        return data;
    }
}
