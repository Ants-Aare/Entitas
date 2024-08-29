using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateContextExtensions
{
    public static void GenerateContextExtensionsOutput(SourceProductionContext context, ExtendedComponentDataWithSystems data)
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
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, $"{contextData.Name}{componentData.Prefix}Extensions"), stringBuilder.ToString());
    }

    static string GetClassContent(ContextData contextData, ComponentData componentData)
    {
        return $$"""
                 partial class {{contextData.Name}} : {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Context
                 {
                 """;
    }

    static string GetUniqueContent(ExtendedComponentDataWithSystems data)
    {
        var componentData = data.ComponentData;
        var contextData = data.ContextData;

        var methodSignature = componentData.GetMethodSignature();
        var methodArguments = componentData.GetMethodArguments();
        var equalityComparer = componentData.GetEqualityComparer();

        var onAddedEvents = new StringBuilder();
        var onSetEvents = new StringBuilder();
        var onChangedEvents = new StringBuilder();
        var onRemovedEvents = new StringBuilder();

        foreach (var systemData in data.SystemDatas)
        {
            var systemCall = $"{systemData.ValidLowerName}.OnEntityTriggered(contextEntity);";
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

                 public {{contextData.Name}} Set{{componentData.Prefix}}({{methodSignature}})
                 {
                     if (!this.IsEnabled)
                     {
                         return this;
                     }

                     if (this.{{componentData.Name}} == null)
                     {
                         this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});
                         {{onAddedEvents}}
                         return this;
                     }

                     if (Has{{componentData.Prefix}}(){{equalityComparer}})
                     {
                        {{onSetEvents}}
                         return this;
                     }
                     var previousComponent = this.{{componentData.Name}};
                     this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});
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
                         {{contextData.Prefix}}Entity[] Indexed{{componentData.Prefix}}Entities = new {{contextData.Prefix}}Entity[{{componentData.IndexMaxSize}}];
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
                         System.Collections.Generic.Dictionary<int, {{contextData.Prefix}}Entity> Indexed{{componentData.Prefix}}Entities = new ();
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
