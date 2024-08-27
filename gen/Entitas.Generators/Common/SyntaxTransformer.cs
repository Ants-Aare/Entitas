using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Entitas.Generators.Common;

public static class SyntaxTransformer
{
    public static T? TransformClassDeclarationTo<T>(GeneratorSyntaxContext context, CancellationToken ct)
        where T : struct, IClassDeclarationResolver
    {
        var instance = new T();
        try
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax, ct);
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
                return null;

            if (!instance.TryResolveClassDeclaration(namedTypeSymbol))
                return null;

            if (instance is IAttributeResolver attributeResolver)
                instance = (T)attributeResolver.ResolveAttributes(namedTypeSymbol, ct);
            // if (instance is IConstructorResolver constructorResolver)
            // constructorResolver.ResolveConstructors(namedTypeSymbol, ct);
            if (instance is IMethodResolver methodResolver)
                instance = (T)methodResolver.ResolveMethods(namedTypeSymbol, ct);
            if (instance is IFieldResolver fieldResolver)
                instance = (T)fieldResolver.ResolveFields(namedTypeSymbol, ct);

            if (instance is IFinalisable<T> finalisable)
                instance = finalisable.Finalise();

            return instance;
        }
        catch
        {
            return null;
        }
    }
}

public interface IFinalisable<T>
    where T : struct, IClassDeclarationResolver
{
    public T Finalise();
}
