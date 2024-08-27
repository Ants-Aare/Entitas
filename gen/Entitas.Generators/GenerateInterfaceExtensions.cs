using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateInterfaceExtensions
{
    public static void GenerateInterfaceExtensionsOutput(SourceProductionContext context, ComponentWithContexts componentWithContexts)
    {
        var componentData = componentWithContexts.ComponentData;
        var contextDatas = componentWithContexts.ContextDatas;
        // var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));

        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateInterfaceExtensions));
            using (new NamespaceBuilder(stringBuilder, componentData.Namespace))
            {
                stringBuilder.AppendLine(GetDefaultContent(componentData, contextDatas));

                if (componentData.IsUnique)
                {
                    stringBuilder.AppendLine(GetUniqueContent(componentData, contextDatas));
                }
                else if (componentData.IndexType != EntityIndexType.None)
                {
                    stringBuilder.AppendLine(GetIndexContent(componentData, contextDatas));
                }

                stringBuilder.AppendLine("}");
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(componentData.Namespace, $"I{componentData.Prefix}EntityExtensions"), stringBuilder.ToString());
    }

    static string GetIndexContent(ComponentData componentData, ImmutableArray<ContextData> contextDatas)
    {
        var methodSignature = componentData.GetComponentValuesMethodSignature();
        var methodArguments = componentData.GetComponentValuesMethodArguments();
        var getIndexedEntity = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"        {x.FullName} {x.Name} => {x.Name}.GetEntityWith{componentData.Prefix}({methodArguments}),"));

        return $$"""
                 public static I{{componentData.Prefix}}Entity GetEntityWith{{componentData.Prefix}}(I{{componentData.Prefix}}Context context, {{methodSignature}})
                     => context switch
                     {
                 {{getIndexedEntity}}
                     _ => default
                     };
                 """;
    }

    static string GetUniqueContent(ComponentData componentData, ImmutableArray<ContextData> contextDatas)
    {
        var methodArguments = componentData.GetComponentValuesMethodArguments();
        var methodSignatureWithLeadingComma = componentData.GetComponentValuesMethodSignatureLeadingComma();
        var contextHasComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Has{componentData.Prefix}(),"));
        var contextGetComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Get{componentData.Prefix}(),"));
        var contextSetComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Set{componentData.Prefix}({methodArguments}),"));
        var contextRemoveComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Remove{componentData.Prefix}(),"));

        return $$"""
                       public static bool Has{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context)
                       => context switch
                       {
                          {{contextHasComponent}}
                           _ => default
                       };
                       public static {{componentData.FullName}} Get{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context)
                       => context switch
                       {
                          {{contextGetComponent}}
                           _ => default
                       };
                       public static I{{componentData.Prefix}}Context Set{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context{{methodSignatureWithLeadingComma}})
                       => context switch
                       {
                          {{contextSetComponent}}
                           _ => default
                       };
                       public static I{{componentData.Prefix}}Context Remove{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context)
                       => context switch
                       {
                          {{contextRemoveComponent}}
                           _ => default
                       };
                 """;
    }

    static string GetDefaultContent(ComponentData componentData, ImmutableArray<ContextData> contextDatas)
    {
        var methodArguments = componentData.GetComponentValuesMethodArguments();
        var methodSignatureWithLeadingComma = componentData.GetComponentValuesMethodSignatureLeadingComma();
        //public static E2GameEntity AsE2GameEntity(this IBoardEntity e) => (E2GameEntity)e;
        var asEntity = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\tpublic static {x.FullPrefix}Entity As{x.Prefix}Entity(this I{componentData.Prefix}Entity e) => ({x.FullPrefix}Entity)e;"));
        var getContext = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Context,"));
        var hasComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Has{componentData.Prefix}(),"));
        var getComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Get{componentData.Prefix}(),"));
        var setComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Set{componentData.Prefix}({methodArguments}),"));
        var removeComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Remove{componentData.Prefix}(),"));
        var createEntity = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullName} {x.Name} => {x.Name}.CreateEntity(),"));

        return $$"""
                 public static class I{{componentData.Prefix}}Extensions
                 {
                 {{asEntity}}
                     public static I{{componentData.Prefix}}Context GetContext(this System.Collections.Generic.IEnumerable<I{{componentData.Prefix}}Entity> entities) => System.Linq.Enumerable.FirstOrDefault(entities)?.GetContext();
                     public static I{{componentData.Prefix}}Context GetContext(this I{{componentData.Prefix}}Entity entity)
                        => entity switch
                        {
                 {{getContext}}
                        _ => default
                        };
                     public static bool Has{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity)
                        => entity switch
                        {
                 {{hasComponent}}
                        _ => default
                        };
                     public static {{componentData.FullName}} Get{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity)
                        => entity switch
                        {
                 {{getComponent}}
                        _ => default
                        };
                     public static I{{componentData.Prefix}}Entity Set{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity{{methodSignatureWithLeadingComma}})
                        => entity switch
                        {
                 {{setComponent}}
                        _ => default
                        };
                     public static I{{componentData.Prefix}}Entity Remove{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity)
                        => entity switch
                        {
                 {{removeComponent}}
                        _ => default
                        };
                     public static I{{componentData.Prefix}}Entity CreateEntity(this I{{componentData.Prefix}}Context context)
                        => context switch
                        {
                 {{createEntity}}
                        _ => default
                        };
                 """;
    }
}
