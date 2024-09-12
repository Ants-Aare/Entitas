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
    public static void GenerateInterfaceExtensionsOutput(SourceProductionContext context, ExtendedComponentData extendedComponentData)
    {
        var componentData = extendedComponentData.ComponentData;
        var contextDatas = extendedComponentData.ContextDatas;
        var methodArguments = componentData.GetMethodArguments();
        var methodSignatureWithLeadingComma = componentData.GetMethodSignatureLeadingComma();

        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateInterfaceExtensions));
            using (new NamespaceBuilder(stringBuilder, componentData.Namespace))
            {
                stringBuilder.AppendLine(GetDefaultContent(componentData, contextDatas, methodArguments, methodSignatureWithLeadingComma));

                if (!componentData.IsUnique)
                {
                    stringBuilder.AppendLine(GetNonUniqueContent(componentData, methodArguments, methodSignatureWithLeadingComma));
                }
                else
                {
                    stringBuilder.AppendLine(GetUniqueContent(componentData, contextDatas, methodArguments, methodSignatureWithLeadingComma));
                }

                if (!componentData.IsUnique && componentData.IndexType != EntityIndexType.None)
                {
                    stringBuilder.AppendLine(GetIndexContent(componentData, contextDatas, methodArguments, methodSignatureWithLeadingComma));
                }


                stringBuilder.AppendLine("}");
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(Templates.FileNameHint(componentData.Namespace, $"I{componentData.Prefix}EntityExtensions"), stringBuilder.ToString());
    }


    static string GetNonUniqueContent(ComponentData componentData, string methodArguments, string methodSignatureWithLeadingComma)
    {
        var setWithSelector = string.Empty;
        if (componentData.Fields.Length > 0)
        {
            var selectorFuncs = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select((x, i) => $"System.Func<T, {x.TypeName}> selector{i}"));
            var selectorCalls = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select((_, i) => $"selector{i}.Invoke(e)"));
            setWithSelector = componentData.Fields.Length == 0 ? string.Empty : $"\n\tpublic static System.Collections.Generic.IEnumerable<T> Set{componentData.Prefix}s<T>(this System.Collections.Generic.IEnumerable<T> entities, {selectorFuncs}) where T : I{componentData.Prefix}Entity\n\t{{\n\t\tforeach (var e in entities)\n\t\te.Set{componentData.Prefix}({selectorCalls});\n\t\treturn entities;\n\t}}";
        }
        return $$"""
                 public static System.Collections.Generic.IEnumerable<bool> Has{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<I{{componentData.Prefix}}Entity> entities) => System.Linq.Enumerable.Select(entities, e => e.Has{{componentData.Prefix}}());
                 public static System.Collections.Generic.IEnumerable<{{componentData.FullName}}> Get{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<I{{componentData.Prefix}}Entity> entities) => System.Linq.Enumerable.Select(entities,e => e.Get{{componentData.Prefix}}());
                 public static System.Collections.Generic.IEnumerable<T> Set{{componentData.Prefix}}<T>(this System.Collections.Generic.IEnumerable<T> entities{{methodSignatureWithLeadingComma}}) where T : I{{componentData.Prefix}}Entity
                 {
                    foreach (var e in entities)
                        e.Set{{componentData.Prefix}}({{methodArguments}});
                    return entities;
                 }{{setWithSelector}}
                 public static System.Collections.Generic.IEnumerable<T> Remove{{componentData.Prefix}}<T>(this System.Collections.Generic.IEnumerable<T> entities)where T : I{{componentData.Prefix}}Entity
                 {
                    foreach (var e in entities)
                        e.Remove{{componentData.Prefix}}();
                    return entities;
                 }
                 """;
    }

    static string GetIndexContent(ComponentData componentData, ImmutableArray<ContextData> contextDatas, string methodArguments, string methodSignature)
    {
        var getIndexedEntity = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"        {x.FullName} {x.Name} => {x.Name}.GetEntityWith{componentData.Prefix}({methodArguments}),"));

        return $$"""
                 public static I{{componentData.Prefix}}Entity GetEntityWith{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context{{methodSignature}})
                     => context switch
                     {
                 {{getIndexedEntity}}
                     _ => default
                     };
                 """;
    }

    static string GetUniqueContent(ComponentData componentData, ImmutableArray<ContextData> contextDatas, string methodArguments, string methodSignatureWithLeadingComma)
    {
        var contextHasComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Has{componentData.Prefix}(),"));
        var contextGetComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Get{componentData.Prefix}(),"));
        var contextSetComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Set{componentData.Prefix}({methodArguments}),"));
        var contextRemoveComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Remove{componentData.Prefix}(),"));

        return $$"""
                       public static bool Has{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<I{{componentData.Prefix}}Entity> entities) => System.Linq.Enumerable.FirstOrDefault(entities)?.Has{{componentData.Prefix}}() ?? false;
                       public static {{componentData.FullName}} Get{{componentData.Prefix}}(this System.Collections.Generic.IEnumerable<I{{componentData.Prefix}}Entity> entities) => System.Linq.Enumerable.FirstOrDefault(entities)?.Get{{componentData.Prefix}}() ?? null;
                       public static System.Collections.Generic.IEnumerable<T> Set{{componentData.Prefix}}<T>(this System.Collections.Generic.IEnumerable<T> entities{{methodSignatureWithLeadingComma}}) where T : I{{componentData.Prefix}}Entity { System.Linq.Enumerable.FirstOrDefault(entities)?.Set{{componentData.Prefix}}({{methodArguments}});return entities;}
                       public static System.Collections.Generic.IEnumerable<T> Remove{{componentData.Prefix}}<T>(this System.Collections.Generic.IEnumerable<T> entities) where T : I{{componentData.Prefix}}Entity { System.Linq.Enumerable.FirstOrDefault(entities)?.Remove{{componentData.Prefix}}();return entities;}

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

    static string GetDefaultContent(ComponentData componentData, ImmutableArray<ContextData> contextDatas, string methodArguments, string methodSignatureWithLeadingComma)
    {
        //public static E2GameEntity AsE2GameEntity(this IBoardEntity e) => (E2GameEntity)e;
        var getContext = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Context,"));
        var hasComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Has{componentData.Prefix}(),"));
        var getComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Get{componentData.Prefix}(),"));
        var setComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\tcase {x.FullPrefix}Entity {x.Prefix}Entity: {x.Prefix}Entity.Set{componentData.Prefix}({methodArguments}); break;"));
        var removeComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\tcase {x.FullPrefix}Entity {x.Prefix}Entity: {x.Prefix}Entity.Remove{componentData.Prefix}(); break;"));
        // var removeComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Remove{componentData.Prefix}(),"));
        var createEntity = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"\t\t\t{x.FullName} {x.Name} => {x.Name}.CreateEntity(),"));
        return $$"""
                 public static class I{{componentData.Prefix}}Extensions
                 {
                     public static I{{componentData.Prefix}}Context GetI{{componentData.Prefix}}Context(this I{{componentData.Prefix}}Entity entity)
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

                     public static T Set{{componentData.Prefix}}<T>(this T entity{{methodSignatureWithLeadingComma}}) where T : I{{componentData.Prefix}}Entity
                     {
                         switch (entity)
                         {
                         {{setComponent}}
                         };
                         return entity;
                     }

                     public static T Remove{{componentData.Prefix}}<T>(this T entity) where T : I{{componentData.Prefix}}Entity
                     {
                         switch (entity)
                         {
                         {{removeComponent}}
                         };
                         return entity;
                     }

                     public static I{{componentData.Prefix}}Entity CreateEntity(this I{{componentData.Prefix}}Context context)
                        => context switch
                        {
                 {{createEntity}}
                        _ => default
                        };

                     public static I{{componentData.Prefix}}Context GetI{{componentData.Prefix}}Context(this System.Collections.Generic.IEnumerable<I{{componentData.Prefix}}Entity> entities) => System.Linq.Enumerable.FirstOrDefault(entities)?.GetI{{componentData.Prefix}}Context();
                 """;
    }
}
