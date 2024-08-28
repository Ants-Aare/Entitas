using System.Linq;
using System.Text;
using Entitas.Generators.Data;

namespace Entitas.Generators.Utility;

public static class ComponentDataExtensions
{
    public static string GetEqualityComparer(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join("", componentData.Fields.Select(field => $" && this.{componentData.Name}.{field.Name} == {field.ValidLowerName}"));

    public static string GetMethodSignatureLeadingComma(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : $", {GetMethodSignature(componentData)}";

    public static string GetMethodArguments(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));

    public static string GetMethodSignature(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));

    public static string GetVariableMethodArguments(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"component.{field.Name}"));

    public static string GetVariableMethodArguments(this ComponentData componentData, string variableName)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(field => $"{variableName}.{field.Name}"));

    public static string GetHashCodeFromMethodArguments(this ComponentData componentData)
    {
        if (componentData.Fields.Length == 1)
            return $"var _hashCode = {componentData.Fields[0].ValidLowerName}.GetHashCode();";

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"int _hashCode = {componentData.Fields[0].ValidLowerName}.GetHashCode();\nunchecked\n{{");
        for (var i = 1; i < componentData.Fields.Length; i++)
        {
            stringBuilder.AppendLine($"_hashCode = (_hashCode * 397) ^ {componentData.Fields[i].ValidLowerName}.GetHashCode();");
        }

        stringBuilder.AppendLine("}");
        return stringBuilder.ToString();
    }
}
