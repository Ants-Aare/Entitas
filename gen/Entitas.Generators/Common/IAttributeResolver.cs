using System.Threading;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Common;

public interface IAttributeResolver
{
    public bool TryResolveAttribute(AttributeData attributeData);
}

public static class AttributeResolverExtension
{
    public static IAttributeResolver ResolveAttributes(this IAttributeResolver attributeResolver, ISymbol symbol, CancellationToken ct = default)
    {
        var attributeDatas = symbol.GetAttributes();
        foreach (var attributeData in attributeDatas)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                attributeResolver.TryResolveAttribute(attributeData);
            }
            catch
            {
            }
        }

        return attributeResolver;
    }
}
