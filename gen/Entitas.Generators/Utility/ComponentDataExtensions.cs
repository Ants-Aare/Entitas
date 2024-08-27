using System.Linq;
using System.Text;
using Entitas.Generators.Data;

namespace Entitas.Generators.Utility;

public static class ComponentDataExtensions
{
    public static string GetComponentValuesEqualityComparer(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join("", componentData.Fields.Select(field => $" && this.{componentData.Name}.{field.Name} == {field.ValidLowerName}"));

    public static string GetComponentValuesMethodSignatureLeadingComma(this ComponentData componentData)
    {
        var methodSignature = GetComponentValuesMethodSignature(componentData);
        return methodSignature == string.Empty ? string.Empty : $", {methodSignature}";
    }

    public static string GetComponentValuesMethodArguments(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));

    public static string GetComponentValuesMethodSignature(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));

    public static string GetComponentValuesRetrieval(this ComponentData componentData)
        => componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"component.{field.ValidLowerName}"));

    public static string GetHashCodeFromMethodArguments(this ComponentData componentData)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("unchecked\n{");
        for (int i = 0; i < componentData.Fields.Length; i++)
        {
            if (i == 0)
                stringBuilder.Append($"var hashCode = {componentData.Fields[i].ValidLowerName}.GetHashCode();");
            else
                stringBuilder.Append($"hashCode = (hashCode * 397) ^ {componentData.Fields[i].ValidLowerName}.GetHashCode();");
        }
        stringBuilder.Append("}");
        return stringBuilder.ToString();
    }
}
