using System;
using Entitas.Generators.Data;

namespace Entitas.Generators.Utility;

public static class SystemDataUtility
{
    public static bool HasMultipleConstraints(this SystemData systemData) => systemData.TriggeredBy.Length + systemData.EntityIs.Length > 1;
    public static bool HasNoConstraints(this SystemData systemData) => systemData.TriggeredBy.Length + systemData.EntityIs.Length == 0;

    public static string GetEntityType(this SystemData systemData)
    {
        if (systemData.HasNoConstraints())
            return string.Empty;
        if (systemData.EntityIsContext())
            return $"{systemData.EntityIs[0].FullName.RemoveSuffix("Context")}Entity";
        if (systemData.EntityIsFeature())
            return $"{systemData.EntityIs[0].NamespaceSpecifier}I{systemData.EntityIs[0].Name}Entity";

        if (systemData.HasMultipleConstraints())
            return $"I{systemData.Name}Entity";
        var componentData = systemData.TriggeredBy.Length > 0
            ? systemData.TriggeredBy[0].component
            : systemData.EntityIs[0];
        return $"{componentData.Namespace.NamespaceClassifier()}I{componentData.Prefix}Entity";
    }

    public static string GetContextType(this SystemData systemData)
    {
        if (systemData.HasNoConstraints())
            return string.Empty;
        if (systemData.EntityIsContext())
            return systemData.EntityIs[0].FullName;
        if (systemData.EntityIsFeature())
            return $"{systemData.EntityIs[0].NamespaceSpecifier}I{systemData.EntityIs[0].Name}Context";

        if (systemData.HasMultipleConstraints())
            return $"I{systemData.Name}Context";
        var componentData = systemData.TriggeredBy.Length > 0
            ? systemData.TriggeredBy[0].component
            : systemData.EntityIs[0];
        return $"{componentData.Namespace.NamespaceClassifier()}I{componentData.Prefix}Context";
    }

    public static bool EntityIsContext(this SystemData systemData)
        => systemData.EntityIs.Length > 0 && systemData.EntityIs[0].FullName.EndsWith("Context", StringComparison.Ordinal);
    public static bool EntityIsFeature(this SystemData systemData)
        => systemData.EntityIs.Length > 0 && systemData.EntityIs[0].FullName.EndsWith("Feature", StringComparison.Ordinal);

    public static bool NeedsCustomInterface(this SystemData systemData) => systemData.HasMultipleConstraints() && !systemData.EntityIsContext();
}
