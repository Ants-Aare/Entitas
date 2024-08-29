using System;
using System.Linq;
using Entitas.Generators.Data;

namespace Entitas.Generators.Utility;

public static class SystemDataUtility
{
    public static bool HasMultipleConstraints(this SystemData systemData) => systemData.TriggeredBy.Length + systemData.EntityIs.Length > 1;
    public static bool HasNoConstraints(this SystemData systemData) => systemData.TriggeredBy.Length + systemData.EntityIs.Length == 0;

    public static string GetEntityType(this ExtendedSystemData data)
    {
        var systemData = data.SystemData;
        if (systemData.HasNoConstraints())
            return string.Empty;
        if (systemData.EntityIsContext())
            return $"{systemData.EntityIs[0].RemoveSuffix("Context")}Entity";
        if (systemData.HasMultipleConstraints())
            return $"I{systemData.Name}Entity";
        var componentData = systemData.TriggeredBy.Length > 0
            ? data.ComponentDatas.FirstOrDefault(x => x.Name == systemData.TriggeredBy[0].component)
            : data.ComponentDatas.FirstOrDefault(x => x.Name == systemData.EntityIs[0]);
        return $"{componentData.Namespace.NamespaceClassifier()}I{componentData.Prefix}Entity";
    }

    public static string GetContextType(this ExtendedSystemData data)
    {
        var systemData = data.SystemData;
        if (systemData.HasNoConstraints())
            return string.Empty;
        if (systemData.EntityIsContext())
            return systemData.EntityIs[0];
        if (systemData.HasMultipleConstraints())
            return $"I{systemData.Name}Context";
        var componentData = systemData.TriggeredBy.Length > 0
            ? data.ComponentDatas.FirstOrDefault(x => x.Name == systemData.TriggeredBy[0].component)
            : data.ComponentDatas.FirstOrDefault(x => x.Name == systemData.EntityIs[0]);
        return $"{componentData.Namespace.NamespaceClassifier()}I{componentData.Prefix}Context";
    }

    public static bool EntityIsContext(this SystemData systemData)
    {
        return systemData.EntityIs.Length > 0 && !systemData.EntityIs[0].StartsWith("I") && systemData.EntityIs[0].EndsWith("Context", StringComparison.Ordinal);
    }

    public static bool NeedsCustomInterface(this SystemData systemData) => systemData.HasMultipleConstraints() && !systemData.EntityIsContext();
}
