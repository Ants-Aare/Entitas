using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Data;

public record struct TypeData(string? Namespace, string FullName, string Name, string? FullPrefix, string? Prefix)
{
    public string NamespaceSpecifier => !string.IsNullOrEmpty(Namespace) ? $"{@Namespace}." : string.Empty;
    public readonly bool Equals(TypeData other) => string.Equals(FullName, other.FullName);

    public readonly override int GetHashCode() => FullName.GetHashCode();

    public static TypeData Create(INamedTypeSymbol namedTypeSymbol, string suffix)
    {
        var @namespace = namedTypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : namedTypeSymbol.ContainingNamespace.ToDisplayString();

        var fullName = namedTypeSymbol.ToDisplayString();
        var name = namedTypeSymbol.Name;

        var fullPrefix = fullName.TryRemoveSuffix(suffix);
        var prefix = name.TryRemoveSuffix(suffix);
        return new TypeData(@namespace, fullName, name, fullPrefix, prefix);
    }
    public static TypeData Create(INamedTypeSymbol namedTypeSymbol)
    {
        var @namespace = namedTypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : namedTypeSymbol.ContainingNamespace.ToDisplayString();

        var fullName = namedTypeSymbol.ToDisplayString();
        var name = namedTypeSymbol.Name;

        return new TypeData(@namespace, fullName, name, null, null);
    }
}
