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
                attributeResolver.ResolveAttributes(namedTypeSymbol, ct);
            // if (instance is IConstructorResolver constructorResolver)
                // constructorResolver.ResolveConstructors(namedTypeSymbol, ct);
            if (instance is IFieldResolver fieldResolver)
                fieldResolver.ResolveFields(namedTypeSymbol, ct);

            return instance;
        }
        catch
        {
            return null;
        }
    }
}
