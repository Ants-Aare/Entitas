using System.Threading;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public interface IClassDeclarationResolver
{
    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol);
}
