using System;
using Entitas.Generators.Data;
using static Entitas.Generators.Utility.StringConstants;

namespace Entitas.Generators.Utility;

public static class ListenerDataUtility
{
    // public static string GetEntityType(this ListenerData listenerData)
    // {
    //     if (listenerData.Events.IsDefaultOrEmpty)
    //         return string.Empty;
    //     if (listenerData.EntityIsContext())
    //         return $"{listenerData.EntityIs[0].FullName.RemoveSuffix(ContextName)}Entity";
    //     if (listenerData.EntityIsFeature())
    //         return $"{listenerData.EntityIs[0].NamespaceSpecifier}I{listenerData.EntityIs[0].Name}Entity";
    //     if (listenerData.Events.Length + listenerData.EntityIs.Length > 1)
    //         return $"I{listenerData.Name}ListenerEntity";
    //
    //     return $"{listenerData.Events[0].Component.NamespaceSpecifier}I{listenerData.Events[0].Component.Prefix}Entity";
    // }
    //
    // public static string GetContextType(this ListenerData listenerData)
    // {
    //     if (listenerData.Events.IsDefaultOrEmpty)
    //         return string.Empty;
    //     if (listenerData.EntityIsContext())
    //         return listenerData.EntityIs[0].FullName;
    //     if (listenerData.EntityIsFeature())
    //         return $"{listenerData.EntityIs[0].NamespaceSpecifier}I{listenerData.EntityIs[0].Name}Context";
    //     if (listenerData.Events.Length + listenerData.EntityIs.Length  > 1)
    //         return $"I{listenerData.Name}ListenerContext";
    //
    //     return $"{listenerData.Events[0].Component.NamespaceSpecifier}I{listenerData.Events[0].Component.Prefix}Context";
    // }
    //
    // public static bool EntityIsContext(this ListenerData listenerData)
    //     => listenerData.EntityIs.Length > 0 && listenerData.EntityIs[0].FullName.EndsWith(ContextName, StringComparison.Ordinal);
    // public static bool EntityIsFeature(this ListenerData listenerData)
    //     => listenerData.EntityIs.Length > 0 && listenerData.EntityIs[0].FullName.EndsWith(FeatureName, StringComparison.Ordinal);
}
