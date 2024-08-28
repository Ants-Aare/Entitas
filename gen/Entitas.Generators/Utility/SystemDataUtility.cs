using Entitas.Generators.Data;

namespace Entitas.Generators.Utility;

public static class SystemDataUtility
{
    public static bool HasMultipleConstraints(this SystemData systemData) => systemData.TriggeredBy.Length + systemData.EntityIs.Length > 1;
    public static string GetEntityType(this SystemData systemData, ComponentData componentData)
    {
        if (systemData.HasMultipleConstraints())
        {
            return $"I{systemData.Name}Entity";
        }
        else
        {
            return $"{componentData.Namespace.NamespaceClassifier()}I{componentData.Prefix}Entity";
        }
    }
}
