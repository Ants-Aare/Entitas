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

public struct GroupData() : IClassDeclarationResolver, IAttributeResolver, IFinalisable<GroupData>, IComparable<GroupData>, IComparable, IEquatable<GroupData>
{
    public TypeData TypeData { get; private set; } = default;
    public string ValidLowerName { get; private set; } = null!;

    public ImmutableArray<TypeData> AnyOf { get; private set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> AllOf { get; private set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> NoneOf { get; private set; } = ImmutableArray<TypeData>.Empty;

    public string? Namespace => TypeData.Namespace;
    public string FullName => TypeData.FullName;
    public string Name => TypeData.Name;
    public string FullPrefix => TypeData.FullPrefix!;
    public string Prefix => TypeData.Prefix!;
    public IEnumerable<TypeData> GetAllTypes => AnyOf.Concat(AllOf).Concat(NoneOf);

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken ct)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: EntityGroupName or EntityGroupAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: EntityGroupName or EntityGroupAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        TypeData = TypeData.Create(symbol, "Group");
        ValidLowerName = Name.ToValidLowerName();
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: AnyOfAttributeName } => TryResolveAnyOfAttribute(attributeData),
            { AttributeClass.Name: AllOfAttributeName } => TryResolveAllOfAttribute(attributeData),
            { AttributeClass.Name: NoneOfAttributeName } => TryResolveNoneOfAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveAnyOfAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        AnyOf = AnyOf.AddRange(values.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName)));
        return true;
    }

    bool TryResolveAllOfAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        AllOf = AllOf.AddRange(values.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName)));
        return true;
    }

    bool TryResolveNoneOfAttribute(AttributeData attributeData)
    {
        var values = attributeData.ConstructorArguments[0].Values;
        NoneOf = NoneOf.AddRange(values.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName)));
        return true;
    }

    public bool ContainsAllNecessaryComponents(ImmutableArray<ComponentData> components)
    {
        var componentNames = components.Select(x => x.TypeData.FullName).ToList();
        if (Enumerable.Any(AllOf, typeData => !componentNames.Contains(typeData.FullName)))
            return false;
        if (Enumerable.Any(AnyOf, typeData => !componentNames.Contains(typeData.FullName)))
            return false;
        return true;
    }

    public bool ContainsAllNecessaryComponents(ContextData contextData)
    {
        var componentNames = contextData.Components.Select(x => x.FullName).ToList();
        if (Enumerable.Any(AllOf, typeData => !componentNames.Contains(typeData.FullName)))
            return false;
        if (Enumerable.Any(AnyOf, typeData => !componentNames.Contains(typeData.FullName)))
            return false;
        return true;
    }


    public bool Equals(GroupData other)
    {
        return TypeData.Equals(other.TypeData) && AnyOf.Equals(other.AnyOf) && AllOf.Equals(other.AllOf) && NoneOf.Equals(other.NoneOf);
    }

    public GroupData? Finalise()
    {
        if (AnyOf.Length == 0 && AllOf.Length == 0 && NoneOf.Length == 0)
            return null;

        return this;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("SystemData:\n");
        try
        {
            stringBuilder.AppendLine($"   {nameof(Namespace)}: {Namespace}")
                .AppendLine($"   {nameof(FullName)}: {FullName}")
                .AppendLine($"   {nameof(Name)}: {Name}")
                .AppendLine($"   {nameof(ValidLowerName)}: {ValidLowerName}");

            if (AnyOf.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(AnyOf)}:");
                foreach (var typeData in AnyOf)
                {
                    stringBuilder.AppendLine($"      {typeData.FullName}");
                }
            }

            if (AllOf.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(AllOf)}:");
                foreach (var typeData in AllOf)
                {
                    stringBuilder.AppendLine($"      {typeData.FullName}");
                }
            }

            if (NoneOf.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(NoneOf)}:");
                foreach (var typeData in NoneOf)
                {
                    stringBuilder.AppendLine($"      {typeData.FullName}");
                }
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        return stringBuilder.ToString();
    }

    public int CompareTo(GroupData other)
    {
        return string.Compare(FullName, other.FullName, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is GroupData other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(GroupData)}");
    }

    public override bool Equals(object? obj)
    {
        return obj is GroupData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = TypeData.GetHashCode();
            hashCode = (hashCode * 397) ^ AnyOf.GetHashCode();
            hashCode = (hashCode * 397) ^ AllOf.GetHashCode();
            hashCode = (hashCode * 397) ^ NoneOf.GetHashCode();
            return hashCode;
        }
    }

    public static IEqualityComparer<GroupData> ComponentComparer { get; } = new ComponentEqualityComparer();

    sealed class ComponentEqualityComparer : IEqualityComparer<GroupData>
    {
        public bool Equals(GroupData x, GroupData y)
        {
            return x.AnyOf.Equals(y.AnyOf) && x.AllOf.Equals(y.AllOf) && x.NoneOf.Equals(y.NoneOf);
        }

        public int GetHashCode(GroupData obj)
        {
            unchecked
            {
                var hashCode = obj.AnyOf.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.AllOf.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.NoneOf.GetHashCode();
                return hashCode;
            }
        }
    }
}
