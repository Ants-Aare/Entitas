using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

public static class GenerateArchetype
{
    public static void GenerateArchetypeOutputs(SourceProductionContext context, ExtendedArchetypeData data)
    {
        var archetypeData = data.ArchetypeData;

        for (var i = 0; i < data.ContextDatas.Length; i++)
        {
            var contextData = data.ContextDatas[i];
            var stringBuilder = new StringBuilder();
            try
            {
                stringBuilder.AppendGenerationWarning(nameof(GenerateArchetype));

                using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
                {
                    stringBuilder.AppendLine(GetContextContent(data, i));
                }

                using (new CommentBuilder(stringBuilder))
                {
                    stringBuilder.AppendLine(archetypeData.ToString());
                }
            }
            catch (Exception e)
            {
                stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
            }

            context.AddSource(StringUtility.FileNameHint(contextData.Namespace, contextData.Name + archetypeData.Name), stringBuilder.ToString());
        }


        var stringBuilder2 = new StringBuilder();
        try
        {
            stringBuilder2.AppendGenerationWarning(nameof(GenerateArchetype));

            using (new NamespaceBuilder(stringBuilder2, archetypeData.TypeData.Namespace))
            {
                stringBuilder2.AppendLine(GetArchetypeContent(data));
            }

            using (new CommentBuilder(stringBuilder2))
            {
                stringBuilder2.AppendLine(archetypeData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder2.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }
        context.AddSource(StringUtility.FileNameHint(archetypeData.TypeData.Namespace, archetypeData.Name), stringBuilder2.ToString());
    }

    static string GetArchetypeContent(ExtendedArchetypeData data)
    {
        var contextInterfaces = string.Join(", ", data.ComponentDatas.Select(x=> $"{x.Namespace}.I{x.TypeData.Prefix}Context"));
        var entityInterfaces = string.Join(", ", data.ComponentDatas.Select(x=> $"{x.Namespace}.I{x.TypeData.Prefix}Entity"));
        return $$"""
                     partial class {{data.ArchetypeData.Name}}{}
                     partial interface I{{data.ArchetypeData.TypeData.Name}}Context : {{contextInterfaces}} {}
                     partial interface I{{data.ArchetypeData.TypeData.Name}}Entity : {{entityInterfaces}} {}
                 """;
    }

    static string GetContextContent(ExtendedArchetypeData data, int index)
    {
        var archetypeData = data.ArchetypeData;
        var contextData = data.ContextDatas[index];

        var arguments = string.Join(", ", data.ComponentDatas
            .Where(componentData => componentData.Fields.Length != 0 && !archetypeData.Components.Any(x => x.HasDefaultValue && x.TypeData == componentData.TypeData))
            .SelectMany(componentData => componentData.Fields.Select(x => $"{x.TypeName} {componentData.Prefix}{x.Name}")));

        var setCallsBuilder = new StringBuilder();
        var removeCallsBuilder = new StringBuilder();
        var isConstraintsBuilder = new StringBuilder();
        for (var i = 0; i < archetypeData.Components.Length; i++)
        {
            var archetypeComponentData = archetypeData.Components[i];
            var componentData = data.ComponentDatas.FirstOrDefault(x=> x.TypeData == archetypeComponentData.TypeData);

            removeCallsBuilder.Append("\n\t\t\t.Remove")
                .Append(componentData.Prefix)
                .Append("()");
            setCallsBuilder.Append("\n\t\t\t.Set")
                .Append(componentData.Prefix)
                .Append('(');
            for (var j = 0; j < componentData.Fields.Length; j++)
            {
                if (j != 0)
                    setCallsBuilder.Append(',');
                if (archetypeComponentData.HasDefaultValue)
                {
                    setCallsBuilder.Append(archetypeComponentData.DefaultValues[j]);
                }
                else
                {
                    setCallsBuilder.Append(componentData.Prefix).Append(componentData.Fields[j].Name);
                }
            }

            if (i != 0)
                isConstraintsBuilder.Append(',');
            isConstraintsBuilder.Append(componentData.Name)
                .Append(": not null");


            setCallsBuilder.Append(')');
        }

        var setCalls = setCallsBuilder.ToString();

        return $$"""
                     partial class {{contextData.Name}} : {{archetypeData.TypeData.Namespace}}.I{{archetypeData.Name}}Context
                     {
                         public {{contextData.Prefix}}Entity Create{{archetypeData.Name}}({{arguments}})
                             => CreateEntity(){{setCalls}};
                     }
                     partial class {{contextData.Prefix}}Entity: {{archetypeData.TypeData.Namespace}}.I{{archetypeData.Name}}Entity
                     {
                         public {{contextData.Prefix}}Entity Set{{archetypeData.Name}}({{arguments}})
                             => this{{setCalls}};

                         public {{contextData.Prefix}}Entity Remove{{archetypeData.Name}}()
                             => this{{removeCallsBuilder}};

                         public bool Is{{archetypeData.Name}}() => this is { {{isConstraintsBuilder}}};
                     }
                 """;
    }
}
