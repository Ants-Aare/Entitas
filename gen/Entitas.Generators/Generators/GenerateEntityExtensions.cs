using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

public sealed class GenerateEntityExtensions
{
    public static void GenerateEntityExtensionsOutput(SourceProductionContext context, ExtendedComponentDataWithSystemsAndGroups data)
    {
        var componentData = data.ComponentData;
        var contextData = data.ContextData;

        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateEntityExtensions));
            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(GetClassContent(contextData, componentData));
                if (componentData.IsUnique)
                    stringBuilder.AppendLine(GetUniqueContent(data));
                else
                    stringBuilder.AppendLine(GetDefaultContent(data));
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(StringUtility.FileNameHint(contextData.Namespace, $"{contextData.Prefix}Entity{componentData.Prefix}Extensions"), stringBuilder.ToString());
    }

    static string GetClassContent(ContextData contextData, ComponentData componentData)
    {
        return $$"""
                 partial class {{contextData.Prefix}}Entity : {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Entity
                 {
                 """;
    }

    static string GetDefaultContent(ExtendedComponentDataWithSystemsAndGroups data)
    {
        var componentData = data.ComponentData;
        var contextData = data.ContextData;
        var methodSignature = componentData.GetMethodSignature();
        var methodArguments = componentData.GetMethodArguments();
        var equalityComparer = componentData.GetEqualityComparer();
        var methodSignatureWithLeadingComma = componentData.GetMethodSignatureLeadingComma();
        var methodArgumentsWithLeadingComma = componentData.GetMethodArgumentsLeadingComma();
        var componentValues = componentData.GetVariableMethodArguments();

        var onAddedEvents = new StringBuilder();
        var onSetEvents = new StringBuilder();
        var onChangedEvents = new StringBuilder();
        var onRemovedEvents = new StringBuilder();

        if (componentData.IndexType != EntityIndexType.None)
        {
            onAddedEvents.AppendLine($"Context.SetIndexed{componentData.Prefix}Entity(this, {methodArguments});");
            onChangedEvents.AppendLine($"Context.SetIndexed{componentData.Prefix}Entity(null, {componentValues});\nContext.SetIndexed{componentData.Prefix}Entity(this, {methodArguments});");
            onRemovedEvents.AppendLine($"Context.SetIndexed{componentData.Prefix}Entity(null, {componentValues});");
        }

        foreach (var systemData in data.SystemDatas)
        {
            var systemCall = $"\t\tContext.{systemData.ValidLowerName}.OnEntityTriggered(this);";
            var (_, eventType) = systemData.TriggeredBy.FirstOrDefault(x => x.component == componentData.TypeData);

            if (eventType.HasFlagFast(ComponentEvent.Added))
                onAddedEvents.AppendLine(systemCall);
            if (eventType.HasFlagFast(ComponentEvent.Changed))
                onChangedEvents.AppendLine(systemCall);
            if (eventType.HasFlagFast(ComponentEvent.Set))
                onSetEvents.AppendLine(systemCall);
            if (eventType.HasFlagFast(ComponentEvent.Removed))
                onRemovedEvents.AppendLine(systemCall);
        }

        foreach (var groupData in data.GroupDatas)
        {
            var hasMultipleConditions = (groupData.AllOf.Length + groupData.AnyOf.Length + groupData.NoneOf.Length) > 1;
            var condition = string.Empty;
            if (hasMultipleConditions)
            {
                var conditions = new List<string>();
                if (groupData.NoneOf.Length > 0)
                    conditions.Add(string.Join(" && ", groupData.NoneOf.Select(x => $"{x.Name} == null")));
                if (groupData.AnyOf.Length > 0)
                    conditions.Add($"({string.Join(" || ", groupData.AnyOf.Select(x => $"{x.Name} != null"))})");
                if (groupData.AllOf.Length > 0)
                    conditions.Add(string.Join(" && ", groupData.AllOf.Select(x => $"{x.Name} != null")));

                condition = string.Join(" && ", conditions);
            }

            var addCall = $"Context.{groupData.ValidLowerName}Dictionary.Add(Id, this);";
            var removeCall = $"Context.{groupData.ValidLowerName}Dictionary.Remove(Id);";

            var onAdded = hasMultipleConditions
                ? $"if({condition}) {addCall}"
                : addCall;

            var onRemoved = hasMultipleConditions
                ? $"if(!({condition})) {removeCall}"
                : removeCall;

            if (groupData.NoneOf.Contains(componentData.TypeData))
            {
                (onAdded, onRemoved) = (onRemoved, onAdded);
            }

            onAddedEvents.AppendLine(onAdded);
            onRemovedEvents.AppendLine(onRemoved);
        }

        var eventListenerDeclarations = new StringBuilder();

        foreach (var eventData in componentData.Events)
        {
            if (eventData.ListenTarget == ListenTarget.Entity)
            {
                if (eventData.AllowMultipleListeners)
                    eventListenerDeclarations.Append("System.Collections.Generic.List<").Append(componentData.TypeData.NamespaceSpecifier).Append("I").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener> z").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).AppendLine("Listeners;");
                else
                    eventListenerDeclarations.Append(componentData.TypeData.NamespaceSpecifier).Append("I").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener z").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).AppendLine("Listener;");

                eventListenerDeclarations.Append("\n\t[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]\n\tpublic void Add").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener(")
                    .Append(componentData.TypeData.NamespaceSpecifier).Append("I").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener listener){z")
                    .Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append(eventData.AllowMultipleListeners ? "Listeners.Add(listener);}" : "Listener = listener;}");

                eventListenerDeclarations.Append("\n\t[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]\n\tpublic void Remove").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener(")
                    .Append(componentData.TypeData.NamespaceSpecifier).Append("I").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener listener){z")
                    .Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append(eventData.AllowMultipleListeners ? "Listeners.Remove(listener);}" : "Listener = null;}");
            }

            var arguments = eventData.ComponentEvent == ComponentEvent.Removed ? string.Empty : methodArgumentsWithLeadingComma;
            var eventCall = eventData switch
            {
                { Execution: EventExecution.Instant, AllowMultipleListeners: true, ListenTarget: ListenTarget.Context } => $"\t\tforeach (var value in Context.z{componentData.Prefix}{eventData.ComponentEvent}Listeners)\n\t\t\tvalue.On{componentData.Prefix}{eventData.ComponentEvent}(this{arguments});",
                { Execution: EventExecution.Instant, AllowMultipleListeners: false, ListenTarget: ListenTarget.Context } => $"\t\tContext.z{componentData.Prefix}{eventData.ComponentEvent}Listener?.On{componentData.Prefix}{eventData.ComponentEvent}(this{arguments});",
                { Execution: EventExecution.Instant, AllowMultipleListeners: true } => $"\t\tforeach (var value in z{componentData.Prefix}{eventData.ComponentEvent}Listeners)\n\t\t\tvalue.On{componentData.Prefix}{eventData.ComponentEvent}(this{arguments});",
                { Execution: EventExecution.Instant, AllowMultipleListeners: false } => $"\t\tz{componentData.Prefix}{eventData.ComponentEvent}Listener?.On{componentData.Prefix}{eventData.ComponentEvent}(this{arguments});",
                // { AllowMultipleListeners: true, ListenTarget: ListenTarget.Context } => $"\t\tforeach (var value in Context.z{componentData.Prefix}{eventData.ComponentEvent}Listeners)\n\t\t\tvalue.{eventData.Component.Prefix}{eventData.ComponentEvent}();",
                // { AllowMultipleListeners: false, ListenTarget: ListenTarget.Context } => $"\t\tContext.z{componentData.Prefix}{eventData.ComponentEvent}Listener?.{eventData.Component.Prefix}{eventData.ComponentEvent}();",
                // { AllowMultipleListeners: true } => $"\t\tforeach (var value in z{componentData.Prefix}{eventData.ComponentEvent}Listeners)\n\t\t\tvalue.{eventData.Component.Prefix}{eventData.ComponentEvent}();",
                // { AllowMultipleListeners: false } => $"\t\tz{componentData.Prefix}{eventData.ComponentEvent}Listener?.{eventData.Component.Prefix}{eventData.ComponentEvent}();",
                _ => String.Empty,
            };

            if (eventData.ComponentEvent.HasFlagFast(ComponentEvent.Added))
                onAddedEvents.AppendLine(eventCall);
            if (eventData.ComponentEvent.HasFlagFast(ComponentEvent.Changed))
                onChangedEvents.AppendLine(eventCall);
            if (eventData.ComponentEvent.HasFlagFast(ComponentEvent.Set))
                onSetEvents.AppendLine(eventCall);
            if (eventData.ComponentEvent.HasFlagFast(ComponentEvent.Removed))
                onRemovedEvents.AppendLine(eventCall);
        }

        if (componentData.IsCleanup)
        {
            onAddedEvents.AppendLine($"{componentData.FullName}.Collector.Add(this);");
        }

        var setWithSelector = string.Empty;
        if (componentData.Fields.Length > 0)
        {
            var selectorFuncs = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select((x, i) => $"System.Func<{contextData.Prefix}Entity, {x.TypeName}> selector{i}"));
            var selectorCalls = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select((_, i) => $"selector{i}.Invoke(e)"));
            setWithSelector = componentData.Fields.Length == 0 ? string.Empty : $"\n\tpublic static System.Collections.Generic.IEnumerable<{contextData.Prefix}Entity> Set{componentData.Prefix}s(this System.Collections.Generic.IEnumerable<{contextData.Prefix}Entity> entities, {selectorFuncs})\n\t{{\n\t\tforeach (var e in entities)\n\t\te.Set{componentData.Prefix}({selectorCalls});\n\t\treturn entities;\n\t}}";
        }

        return $$"""
                     [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                     public {{componentData.FullName}} {{componentData.Name}};
                     {{eventListenerDeclarations}}

                     public bool Has{{componentData.Prefix}}() => this.{{componentData.Name}} != null;

                     public {{componentData.FullName}} Get{{componentData.Prefix}}() => this.{{componentData.Name}};

                     public {{contextData.Prefix}}Entity Set{{componentData.Prefix}}({{methodSignature}})
                     {
                         if (!this.IsEnabled)
                         {
                             return this;
                         }

                         if (this.{{componentData.Name}} == null)
                         {
                             this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});

                             //OnAddedEvents (Method was called and a new Component was added):
                             {{onAddedEvents}}

                             return this;
                         }

                         if (Has{{componentData.Prefix}}(){{equalityComparer}})
                         {
                             //OnSetEvents (Method was called but component values are the same as before):
                             {{onSetEvents}}

                             return this;
                         }

                         var component = this.{{componentData.Name}};
                         this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});

                         //OnChangedEvents: (Method was called and component Values changed):
                         {{onChangedEvents}}

                         {{componentData.FullName}}.DestroyComponent(component);
                         return this;
                     }

                     public {{contextData.Prefix}}Entity Remove{{componentData.Prefix}}()
                     {
                         if (!this.IsEnabled)
                         {
                             return this;
                         }

                         if (this.{{componentData.Name}} == null)
                         {
                             return this;
                         }

                         var component = this.{{componentData.Name}};
                         this.{{componentData.Name}} = null;

                         //OnRemovedEvents (Component was removed):
                         {{onRemovedEvents}}

                         {{componentData.FullName}}.DestroyComponent(component);
                         return this;
                     }
                 }

                 public static class {{contextData.Prefix}}Entity{{componentData.Prefix}}Extensions
                 {
                     public static {{contextData.FullPrefix}}Entity As{{contextData.Prefix}}Entity(this {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Entity e) => ({{contextData.FullPrefix}}Entity)e;

                     public static System.Collections.Generic.IEnumerable<bool> Has{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities) => System.Linq.Enumerable.Select(entities, e => e.Has{{componentData.Prefix}}());
                     public static System.Collections.Generic.IEnumerable<{{componentData.FullName}}> Get{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities) => System.Linq.Enumerable.Select(entities,e => e.Get{{componentData.Prefix}}());
                     public static System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Set{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities{{methodSignatureWithLeadingComma}})
                     {
                        foreach (var e in entities)
                            e.Set{{componentData.Prefix}}({{methodArguments}});
                        return entities;
                     }{{setWithSelector}}
                     public static System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Remove{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities)
                     {
                        foreach (var e in entities)
                            e.Remove{{componentData.Prefix}}();
                        return entities;
                     }
                 }
                 """;
    }

    static string GetUniqueContent(ExtendedComponentDataWithSystemsAndGroups data)
    {
        var componentData = data.ComponentData;
        var contextData = data.ContextData;
        var methodSignature = componentData.GetMethodSignature();
        var methodArguments = componentData.GetMethodArguments();
        var methodSignatureWithLeadingComma = componentData.GetMethodSignatureLeadingComma();
        return $$"""
                     public bool Has{{componentData.Prefix}}() => this.Context.Has{{componentData.Prefix}}();
                     public {{componentData.FullName}} Get{{componentData.Prefix}}() => this.Context.Get{{componentData.Prefix}}();
                     public {{contextData.Prefix}}Entity Set{{componentData.Prefix}}({{methodSignature}}) { this.Context.Set{{componentData.Prefix}}({{methodArguments}}); return this;}
                     public {{contextData.Prefix}}Entity Remove{{componentData.Prefix}}(){ this.Context.Remove{{componentData.Prefix}}(); return this;}
                 }

                 public static class {{contextData.Prefix}}Entity{{componentData.Prefix}}Extensions
                 {
                     public static {{contextData.FullPrefix}}Entity As{{contextData.Prefix}}Entity(this {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Entity e) => ({{contextData.FullPrefix}}Entity)e;

                     public static bool Has{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities) => entities.GetContext()?.Has{{componentData.Prefix}}() ?? false;
                     public static {{componentData.FullName}} Get{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities) => entities.GetContext()?.Get{{componentData.Prefix}}();
                     public static System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Set{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities{{methodSignatureWithLeadingComma}}) { entities.GetContext()?.Set{{componentData.Prefix}}({{methodArguments}}); return entities;}
                     public static System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Remove{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities) { entities.GetContext()?.Remove{{componentData.Prefix}}(); return entities;}
                 }
                 """;
    }
}
