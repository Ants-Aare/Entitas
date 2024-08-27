using System.Threading;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public interface IMethodResolver
{
    public bool TryResolveMethod(IMethodSymbol methodSymbol);
}

public static class MethodResolverExtension
{
    public static IMethodResolver ResolveMethods(this IMethodResolver methodResolver, INamedTypeSymbol namedTypeSymbol, CancellationToken ct)
    {
        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (member is null || member.IsStatic || member.IsAbstract || member.IsExtern || member.IsImplicitlyDeclared)
                continue;
            if (!member.CanBeReferencedByName || member is not IMethodSymbol methodSymbol)
                continue;

            ct.ThrowIfCancellationRequested();

            if (methodResolver.TryResolveMethod(methodSymbol) == false)
                return methodResolver;
        }

        return methodResolver;
    }
}
