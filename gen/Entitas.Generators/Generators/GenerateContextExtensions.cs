using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

public sealed class GenerateContextExtensions
{
    public static void GenerateContextExtensionsOutput(SourceProductionContext context, ExtendedComponentDataWithSystemsAndGroups data)
    {
        var componentData = data.ComponentData;
        var contextData = data.ContextData;

        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateContextExtensions));
            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(GetClassContent(contextData, componentData));

                if (componentData.IsUnique)
                {
                    stringBuilder.AppendLine(GetUniqueContent(data));
                }
                else if (componentData.IndexType == EntityIndexType.Array || componentData.IndexType == EntityIndexType.Dictionary)
                {
                    stringBuilder.AppendLine(GetIndexContent(contextData, componentData));
                }

                stringBuilder.AppendLine("}");
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(StringUtility.FileNameHint(contextData.Namespace, $"{contextData.Name}{componentData.Prefix}Extensions"), stringBuilder.ToString());
    }

    static string GetClassContent(ContextData contextData, ComponentData componentData)
    {
        return $$"""
                 partial class {{contextData.Name}} : {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Context
                 {
                 """;
    }

    static string GetUniqueContent(ExtendedComponentDataWithSystemsAndGroups data)
    {
        var componentData = data.ComponentData;
        var contextData = data.ContextData;

        var methodSignature = componentData.GetMethodSignature();
        var methodArguments = componentData.GetMethodArguments();
        var methodArgumentsWithLeadingComma = componentData.GetMethodArgumentsLeadingComma();
        var equalityComparer = componentData.GetEqualityComparer();

        var onAddedEvents = new StringBuilder();
        var onSetEvents = new StringBuilder();
        var onChangedEvents = new StringBuilder();
        var onRemovedEvents = new StringBuilder();

        foreach (var systemData in data.SystemDatas)
        {
            var systemCall = $"{systemData.ValidLowerName}.OnEntityTriggered(contextEntity);";
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

        var eventListenerDeclarations = new StringBuilder();

        foreach (var eventData in componentData.Events)
        {
            if (eventData.ListenTarget == ListenTarget.Context)
            {
                if(eventData.AllowMultipleListeners)
                    eventListenerDeclarations.Append("public System.Collections.Generic.List<").Append(componentData.TypeData.NamespaceSpecifier).Append("I").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener> z").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).AppendLine("Listeners;");
                else
                    eventListenerDeclarations.Append("public ").Append(componentData.TypeData.NamespaceSpecifier).Append("I").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).Append("Listener z").Append(eventData.Component.Prefix).Append(eventData.ComponentEvent).AppendLine("Listener;");
            }

            var arguments = eventData.ComponentEvent == ComponentEvent.Removed ? string.Empty : methodArgumentsWithLeadingComma;
            var eventCall = eventData switch
            {
                { Execution: EventExecution.Instant, AllowMultipleListeners: true } => $"\t\tforeach (var value in z{componentData.Prefix}{eventData.ComponentEvent}Listeners)\n\t\t\tvalue.On{componentData.Prefix}{eventData.ComponentEvent}(this{arguments});",
                { Execution: EventExecution.Instant, AllowMultipleListeners: false } => $"\t\tz{componentData.Prefix}{eventData.ComponentEvent}Listener?.On{componentData.Prefix}{eventData.ComponentEvent}(this{arguments});",
                // { AllowMultipleListeners: true } => $"\t\tforeach (var value in z{componentData.Prefix}{eventData.ComponentEvent}Listeners)\n\t\t\tvalue.{eventData.Component.Prefix}{eventData.ComponentEvent}();",
                // { AllowMultipleListeners: false } => $"\t\tz{componentData.Prefix}{eventData.ComponentEvent}Listener?.{eventData.Component.Prefix}{eventData.ComponentEvent}();",
                _=> String.Empty,
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

        return $$"""
                 [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                 public {{componentData.FullName}} {{componentData.Name}};
                 {{eventListenerDeclarations}}

                 public bool Has{{componentData.Prefix}}() => this.{{componentData.Name}} != null;

                 public {{componentData.FullName}} Get{{componentData.Prefix}}() => this.{{componentData.Name}};

                 public {{contextData.Name}} Set{{componentData.Prefix}}({{methodSignature}})
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
                     var previousComponent = this.{{componentData.Name}};
                     this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});

                     //OnChangedEvents: (Method was called and component Values changed):
                     {{onChangedEvents}}

                     {{componentData.FullName}}.DestroyComponent(previousComponent);
                     return this;
                 }

                 public {{contextData.Name}} Remove{{componentData.Prefix}}()
                 {
                     if (!this.IsEnabled)
                     {
                         return this;
                     }

                     if (this.{{componentData.Name}} == null)
                     {
                         return this;
                     }

                     var previousComponent = this.{{componentData.Name}};
                     this.{{componentData.Name}} = null;

                     //OnRemovedEvents (Component was removed):
                     {{onRemovedEvents}}

                     {{componentData.FullName}}.DestroyComponent(previousComponent);
                     return this;
                 }
                 """;
    }

    static string GetIndexContent(ContextData contextData, ComponentData componentData)
    {
        var methodSignature = componentData.GetMethodSignature();
        var methodArguments = componentData.GetMethodArguments();

        switch (componentData.IndexType)
        {
            case EntityIndexType.Array:
                var getIndexMethod = componentData.GetIndexMethod!;
                var method = $"static int Get{componentData.Prefix}Index({methodSignature}){getIndexMethod.Substring(1 + getIndexMethod.IndexOf(')'))}";

                return $$"""
                         public System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Indexed{{componentData.Prefix}}EntitiesGroup => (System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity>)Indexed{{componentData.Prefix}}Entities;
                         [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                         public {{contextData.Prefix}}Entity[] Indexed{{componentData.Prefix}}Entities = new {{contextData.Prefix}}Entity[{{componentData.IndexMaxSize}}];

                         [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                         {{method}}
                         public {{contextData.Prefix}}Entity GetEntityWith{{componentData.Prefix}}({{methodSignature}})
                         {
                             int i = Get{{componentData.Prefix}}Index({{methodArguments}});
                             if(i >= {{componentData.IndexMaxSize}})
                                return null;
                             return Indexed{{componentData.Prefix}}Entities[i];
                         }

                         public void SetIndexed{{componentData.Prefix}}Entity({{contextData.Prefix}}Entity entity, {{methodSignature}})
                         {
                             int i = Get{{componentData.Prefix}}Index({{methodArguments}});
                             if(i >= {{componentData.IndexMaxSize}})
                                return;
                             Indexed{{componentData.Prefix}}Entities[i] = entity;
                         }
                         """;
            case EntityIndexType.Dictionary:
                var hashCodeFromMethodArguments = componentData.GetHashCodeFromMethodArguments();
                return $$"""
                         public System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Indexed{{componentData.Prefix}}EntitiesGroup => Indexed{{componentData.Prefix}}Entities.Values;
                         [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                         public System.Collections.Generic.Dictionary<int, {{contextData.Prefix}}Entity> Indexed{{componentData.Prefix}}Entities = new ();
                         public {{contextData.Prefix}}Entity GetEntityWith{{componentData.Prefix}}({{methodSignature}})
                         {
                         {{hashCodeFromMethodArguments}};
                             if(Indexed{{componentData.Prefix}}Entities.TryGetValue(_hashCode, out var indexedEntity))
                                 return indexedEntity.IsEnabled ? indexedEntity : null;
                            else
                                return null;
                         }

                         public void SetIndexed{{componentData.Prefix}}Entity({{contextData.Prefix}}Entity entity, {{methodSignature}})
                         {
                         {{hashCodeFromMethodArguments}};
                             Indexed{{componentData.Prefix}}Entities[_hashCode] = entity;
                         }
                         """;
            case EntityIndexType.None:
                return string.Empty;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
