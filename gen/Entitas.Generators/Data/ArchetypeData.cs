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

public struct ArchetypeData() : IClassDeclarationResolver, IAttributeResolver, IFinalisable<ArchetypeData>
{
    public TypeData TypeData { get; private set; } = default;

    public ImmutableArray<ArchetypeComponentData> Components = ImmutableArray<ArchetypeComponentData>.Empty;

    // public ImmutableArray<TypeData> Contexts = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> Features { get; private set; } = ImmutableArray<TypeData>.Empty;

    readonly HashSet<TypeData> _features = new();
    readonly Dictionary<string, ArchetypeComponentData> _components = new();
    public string Name => TypeData.Name;

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken ct)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: ArchetypeName or ArchetypeAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: ArchetypeName or ArchetypeAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        TypeData = TypeData.Create(symbol, ArchetypeName);
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: WithComponentsAttributeName } => TryResolveWithComponentsAttribute(attributeData),
            { AttributeClass.Name: WithComponentAttributeName } => TryResolveWithComponentAttribute(attributeData),
            { AttributeClass.Name: WithFeaturesAttributeName } => TryResolveFeaturesAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveWithComponentsAttribute(AttributeData attributeData)
    {
        var typedConstants = attributeData.ConstructorArguments[0].Values;
        var typeDatas = typedConstants.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName));
        foreach (var typeData in typeDatas)
        {
            if (!_components.ContainsKey(typeData.FullName))
                _components[typeData.FullName] = new ArchetypeComponentData(typeData, false, ImmutableArray<string>.Empty);
        }

        return true;
    }

    bool TryResolveWithComponentAttribute(AttributeData attributeData)
    {
        var typeData = TypeData.Create((INamedTypeSymbol)attributeData.ConstructorArguments[0].Value!, ComponentName);
        var hasDefaultValues = (bool)(attributeData.ConstructorArguments[1].Value ?? false);

        var syntaxReference = attributeData.ApplicationSyntaxReference;

        var attributeArgumentListSyntax = (syntaxReference?.GetSyntax().NormalizeWhitespace() as AttributeSyntax)?.ArgumentList?.Arguments.Skip(2);
        // Test = attributeArgumentListSyntax == null ? string.Empty : string.Join(", ", attributeArgumentListSyntax.Select(x => x.ToFullString()));
        var defaultValues = ImmutableArray<string>.Empty; //hasDefaultValues ? attributeData.ConstructorArguments[2].Values.Where(x => !x.IsNull).Select(x => x.ToString()).ToImmutableArray() : ImmutableArray<string>.Empty;
        if (attributeArgumentListSyntax == null)
            hasDefaultValues = false;
        else
            defaultValues = attributeArgumentListSyntax.Select(x => x.ToString()).ToImmutableArray();
        var archetypeData = new ArchetypeComponentData(typeData, hasDefaultValues, defaultValues);

        if (_components.TryGetValue(typeData.FullName, out var value))
        {
            if (!value.HasDefaultValue)
                _components[typeData.FullName] = archetypeData;
        }
        else
        {
            _components[typeData.FullName] = archetypeData;
        }

        return true;
    }

    bool TryResolveFeaturesAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        _features.UnionWith(values
            .Where(x => x.Value is INamedTypeSymbol)
            .Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, FeatureName)));
        return true;
    }

    public ArchetypeData? Finalise()
    {
        Components = _components.Select(x => x.Value).ToImmutableArray();
        Features = _features.ToImmutableArray();
        _components.Clear();
        _features.Clear();
        return this;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("ComponentData:\n");
        try
        {
            stringBuilder.AppendLine($"   Namespace: {TypeData.Namespace}")
                .AppendLine($"   FullName: {TypeData.FullName}")
                .AppendLine($"   {nameof(Name)}: {Name}")
                .AppendLine($"   FullPrefix: {TypeData.FullPrefix}")
                .AppendLine($"   Prefix: {TypeData.Prefix}");

            if (Features.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(Features)}:");
                foreach (var feature in Features)
                {
                    stringBuilder.AppendLine($"      {feature.ToString()}");
                }
            }
            else
            {
                stringBuilder.AppendLine($"      This Component doesn't have any fields.");
            }

            if (Components.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(Components)}:");
                foreach (var componentData in Components)
                {
                    stringBuilder.AppendLine($"      {componentData.TypeData.FullName}, HasDefaultValue: {componentData.HasDefaultValue}, DefaultValues: {string.Join(", ", componentData.DefaultValues)}");
                }
            }

            // stringBuilder.AppendLine(Test);
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        return stringBuilder.ToString();
    }
}

public record struct ArchetypeComponentData(TypeData TypeData, bool HasDefaultValue, ImmutableArray<string> DefaultValues) : IComparable<ArchetypeComponentData>, IComparable
{
    public int CompareTo(ArchetypeComponentData other)
    {
        var hasDefault = HasDefaultValue.CompareTo(other.HasDefaultValue);
        return hasDefault == 0 ? string.Compare(TypeData.Prefix, other.TypeData.Prefix, StringComparison.Ordinal) : hasDefault;
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is ArchetypeComponentData other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(ArchetypeComponentData)}");
    }
}
