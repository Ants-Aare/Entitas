using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateEntityExtensions
{
    public static void GenerateEntityExtensionsOutput(SourceProductionContext context, ExtendedComponentDataWithSystems data)
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
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, $"{contextData.Prefix}Entity{componentData.Prefix}Extensions"), stringBuilder.ToString());
    }

    static string GetClassContent(ContextData contextData, ComponentData componentData)
    {
        return $$"""
                 partial class {{contextData.Prefix}}Entity : {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Entity
                 {
                 """;
    }

    static string GetDefaultContent(ExtendedComponentDataWithSystems data)
    {
        var componentData = data.ComponentData;
        var contextData = data.ContextData;
        var methodSignature = componentData.GetMethodSignature();
        var methodArguments = componentData.GetMethodArguments();
        var equalityComparer = componentData.GetEqualityComparer();
        var methodSignatureWithLeadingComma = componentData.GetMethodSignatureLeadingComma();
        var componentValues = componentData.Fields.Length == 0 ? string.Empty : componentData.GetVariableMethodArguments();

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
            var systemCall = $"Context.{systemData.ValidLowerName}.OnEntityTriggered(this);";
            var (_, eventType) = systemData.TriggeredBy.FirstOrDefault(x => x.component == componentData.Name);
            switch (eventType)
            {
                case EventType.Added:
                    onAddedEvents.AppendLine(systemCall);
                    onSetEvents.AppendLine(systemCall);
                    onChangedEvents.AppendLine(systemCall);
                    break;
                case EventType.Removed:
                    onRemovedEvents.AppendLine(systemCall);
                    break;
                case EventType.AddedOrRemoved:
                    onAddedEvents.AppendLine(systemCall);
                    onSetEvents.AppendLine(systemCall);
                    onChangedEvents.AppendLine(systemCall);
                    onRemovedEvents.AppendLine(systemCall);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return $$"""
                     {{componentData.FullName}} {{componentData.Name}};

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
                             //OnAddedEvents:
                             {{onAddedEvents}}
                             return this;
                         }

                         if (Has{{componentData.Prefix}}(){{equalityComparer}})
                         {
                             //OnChangedEvents(but is the same as before):
                             {{onSetEvents}}
                             return this;
                         }

                         var component = this.{{componentData.Name}};
                         this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});

                         //OnChangedEvents:
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

                         //OnRemovedEvents:
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
                     public static System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Set{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities{{methodSignatureWithLeadingComma}}) => System.Linq.Enumerable.Select(entities,e => e.Set{{componentData.Prefix}}({{methodArguments}}));
                     public static System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> Remove{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities) => System.Linq.Enumerable.Select(entities,e => e.Remove{{componentData.Prefix}}());
                 }
                 """;
    }

    static string GetUniqueContent(ExtendedComponentDataWithSystems data)
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
