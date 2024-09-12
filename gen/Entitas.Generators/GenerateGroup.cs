using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public static class GenerateGroup
{
    public static void GenerateGroupOutput(SourceProductionContext context, GroupData groupData)
    {
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            stringBuilder.AppendGenerationWarning(nameof(GenerateGroup));
            using (new NamespaceBuilder(stringBuilder, groupData.Namespace))
            {
                stringBuilder.AppendLine(GetContent(groupData));
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(groupData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(Templates.FileNameHint(groupData.Namespace, groupData.Name), stringBuilder.ToString());
    }

    static string GetContent(GroupData groupData)
    {
        return $$"""
                 public sealed partial class {{groupData.Name}}
                 {
                 }
                 """;
    }

    public static void GenerateGroupExtensionsOutput(SourceProductionContext context, ExtendedGroupData data)
    {
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            stringBuilder.AppendGenerationWarning(nameof(GenerateGroup));
            using (new NamespaceBuilder(stringBuilder, data.GroupData.Namespace))
            {
                stringBuilder.AppendLine(GetContent(data));
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(data.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(Templates.FileNameHint(data.GroupData.Namespace, $"{data.GroupData.Name}Extensions"), stringBuilder.ToString());
    }

    static string GetContent(ExtendedGroupData data)
    {
        var groupData = data.GroupData;
        var contextDatas = data.ContextDatas;
        var allTypes = groupData.GetAllTypes.ToList();
        var contextConstraints = string.Join(", ", allTypes.Select(x=> $"{x.NamespaceSpecifier}I{x.Prefix}Context"));
        var entityConstraints = string.Join(", ", allTypes.Select(x=> $"{x.NamespaceSpecifier}I{x.Prefix}Entity"));
        var contextGetGroups = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => (System.Collections.Generic.IEnumerable<TEntity>){x.Name}.{groupData.Name},"));
        var entityGetGroups = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Name} => (System.Collections.Generic.IEnumerable<TEntity>){x.Name}.Context.{groupData.Name},"));

        return $$"""
                 public static class {{groupData.Name}}Extensions
                 {
                    public static System.Collections.Generic.IEnumerable<TEntity> Get{{groupData.Name}}<TContext, TEntity>(this TContext context)
                        where TContext : {{contextConstraints}}
                        where TEntity : {{entityConstraints}}
                        => context switch
                        {
                        {{contextGetGroups}}
                             _ => default
                        };

                 public static System.Collections.Generic.IEnumerable<TEntity> Get{{groupData.Name}}<TEntity>(this System.Collections.Generic.IEnumerable<TEntity> entities)
                     where TEntity : {{entityConstraints}}
                     => System.Linq.Enumerable.FirstOrDefault(entities) switch
                     {
                     {{entityGetGroups}}
                          _ => default
                     };
                 }
                 """;
    }
}
