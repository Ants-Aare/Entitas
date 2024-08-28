using System;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Entitas.Generators.Data;

public struct FieldData : /*IAttributeResolver,*/ IFieldResolver, IEquatable<FieldData>
{
    public string TypeName { get; private set; }
    public string Name { get; private set; }
    public string ValidLowerName { get; private set; }
    // public EntityIndexType IndexType { get; private set; } = EntityIndexType.None;

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
        ValidLowerName = Name.ToValidLowerName();
        return true;
    }

    // public bool TryResolveAttribute(AttributeData attributeData)
    // {
    //     if (attributeData is { AttributeClass.Name: IndexedAttributeName })
    //     {
    //         var typedConstant = attributeData.ConstructorArguments.FirstOrDefault().Value;
    //         if (typedConstant == null)
    //             return true;
    //         IndexType = (EntityIndexType)typedConstant;
    //     }
    //
    //     return true;
    // }

    public override string ToString()
    {
        return $"FieldData: {TypeName} {Name} {ValidLowerName}";
    }
    public bool Equals(FieldData other)
    {
        return TypeName == other.TypeName && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        return obj is FieldData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (TypeName.GetHashCode() * 397) ^ Name.GetHashCode();
        }
    }
}
