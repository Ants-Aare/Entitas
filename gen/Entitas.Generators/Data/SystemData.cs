using System.Linq;
using System.Threading;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.StringConstants;

namespace Entitas.Generators.Data;

public struct SystemData : IClassDeclarationResolver, IAttributeResolver
{
    public static bool SyntaxFilter(SyntaxNode node, CancellationToken ct)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: InitializeSystemName or InitializeSystemAttributeName or ReactiveSystemName or ReactiveSystemAttributeName or ExecuteSystemName or ExecuteSystemAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: InitializeSystemName or InitializeSystemAttributeName or ReactiveSystemName or ReactiveSystemAttributeName or ExecuteSystemName or ExecuteSystemAttributeName },
                   }
               });


    public bool TryResolveAttribute(AttributeData attributeData)
    {
        throw new System.NotImplementedException();
    }

    public bool TryResolveClassDeclaration(INamedTypeSymbol namedTypeSymbol)
    {
        throw new System.NotImplementedException();
    }
}
