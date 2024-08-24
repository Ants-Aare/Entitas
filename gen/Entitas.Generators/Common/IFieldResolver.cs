using System.Threading;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public interface IFieldResolver
{
    public bool TryResolveField(IFieldSymbol fieldSymbol);

}

public static class FieldResolverExtension
{
    public static bool ResolveFields(this IFieldResolver attributeResolver, INamedTypeSymbol namedTypeSymbol, CancellationToken ct)
    {
        var memberSymbols = namedTypeSymbol.GetMembers();
        foreach (var symbol in memberSymbols)
        {
            ct.ThrowIfCancellationRequested();

            if(symbol.IsStatic || symbol.IsAbstract || symbol.IsExtern || symbol.IsImplicitlyDeclared)
                continue;
            if(!symbol.CanBeReferencedByName || symbol is not IFieldSymbol fieldSymbol)
                continue;

            var success = attributeResolver.TryResolveField(fieldSymbol);
            if (!success)
                return false;
        }

        return true;
    }
}
