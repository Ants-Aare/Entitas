using System.Linq;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Entitas.Generators.StringConstants;

namespace Entitas.Generators.Data;

public struct FieldData : IAttributeResolver, IFieldResolver
{
    public string TypeName { get; private set; }
    public string Name { get; private set; }
    public string ValidLowerName { get; private set; }
    public EntityIndexType IndexType { get; private set; } = EntityIndexType.None;

    public FieldData()
    {
        TypeName = null!;
        Name = null!;
        ValidLowerName = null!;
    }

    public bool TryResolveField(IFieldSymbol fieldSymbol)
    {
        TypeName = fieldSymbol.Type is IArrayTypeSymbol arrayType
            ? arrayType.ToDisplayString().Replace("*", string.Empty)
            : fieldSymbol.Type.ToDisplayString();

        Name = fieldSymbol.Name;
        ValidLowerName = ToValidLowerName(Name);
        return true;
    }

    static string ToValidLowerName(string value)
    {
        var lowerFirst = char.ToLower(value[0]) + value.Substring(1);
        return SyntaxFacts.GetKeywordKind(lowerFirst) == SyntaxKind.None
            ? lowerFirst
            : $"@{lowerFirst}";
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        if (attributeData is { AttributeClass.Name: IndexedAttributeName })
        {
            var typedConstant = attributeData.ConstructorArguments.FirstOrDefault().Value;
            if (typedConstant == null)
                return true;
            IndexType = (EntityIndexType)typedConstant;
        }

        return true;
    }

    public override string ToString()
    {
        return $"FieldData: Type:{TypeName} Name:{Name} {ValidLowerName} {IndexType}";
    }
}

public enum EntityIndexType
{
    Array,
    Dictionary,
    None,
}
