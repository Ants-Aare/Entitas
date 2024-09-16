using System.Threading;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public interface IConstructorResolver
{
    public bool TryResolveConstructor(IMethodSymbol constructorMethod);
}

public static class ConstructorResolverExtension
{
    public static IConstructorResolver ResolveConstructors(this IConstructorResolver constructorResolver, INamedTypeSymbol namedTypeSymbol, CancellationToken ct)
    {
        foreach (var constructor in namedTypeSymbol.Constructors)
        {
            if (constructor is null)
                continue;
            ct.ThrowIfCancellationRequested();

            if (constructorResolver.TryResolveConstructor(constructor) == false)
                return constructorResolver;
        }

        return constructorResolver;
    }
}
