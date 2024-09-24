using System;
using System.Collections.Generic;
using Entitas.Generators.Common;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Data;

public struct FieldData : /*IAttributeResolver,*/ IFieldResolver, IEquatable<FieldData>
{
    public string TypeName { get; private set; }
    public string Name { get; private set; }
    public string? ValidLowerName { get; private set; }
    public bool IsTypeAnInterface { get; private set; }
    // public EntityIndexType IndexType { get; private set; } = EntityIndexType.None;

    public FieldData()
    {
        TypeName = null!;
        Name = null!;
        ValidLowerName = null;
    }

    public FieldData(string typeName, string name)
    {
        TypeName = typeName;
        Name = name;
        ValidLowerName = null;
    }

    public bool TryResolveField(IFieldSymbol fieldSymbol)
    {
        TypeName = fieldSymbol.Type is IArrayTypeSymbol arrayType
            ? arrayType.ToDisplayString().Replace("*", string.Empty)
            : fieldSymbol.Type.ToDisplayString();

        Name = fieldSymbol.Name;
        ValidLowerName = Name.ToValidLowerName();
        IsTypeAnInterface = fieldSymbol.Type.TypeKind == TypeKind.Interface;
        return true;
    }

    public override string ToString()
    {
        return $"FieldData: {TypeName} {Name} {ValidLowerName}";
    }

    public bool Equals(FieldData other)
    {
        return TypeName == other.TypeName
               && Name == other.Name;
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

    public static IEqualityComparer<FieldData> TypeAndNameComparer { get; } = new TypeAndNameEqualityComparer();

    sealed class TypeAndNameEqualityComparer : IEqualityComparer<FieldData>
    {
        public bool Equals(FieldData x, FieldData y)
        {
            return x.TypeName == y.TypeName && x.Name == y.Name;
        }

        public int GetHashCode(FieldData obj)
        {
            unchecked
            {
                return (obj.TypeName.GetHashCode() * 397) ^ obj.Name.GetHashCode();
            }
        }
    }
}
