using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateComponent
{
    public static void GenerateComponentOutput(SourceProductionContext context, ComponentData componentData)
    {
            context.CancellationToken.ThrowIfCancellationRequested();
        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateComponent));

            using (new NamespaceBuilder(stringBuilder, componentData.Namespace))
            {
                stringBuilder.AppendLine($"public interface I{componentData.Prefix}Context/* : Entitas.IContext */{{ }} \npublic interface I{componentData.Prefix}Entity : Entitas.IEntity {{ }} \n");
                stringBuilder.AppendLine(GetContent(componentData));
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(componentData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(componentData.Namespace, componentData.Name), stringBuilder.ToString());
    }

    static string GetContent(ComponentData componentData)
    {
        var methodSignature = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
        var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));
        var ctorAssignments = componentData.Fields.Length == 0 ? string.Empty : string.Join("\n", componentData.Fields.Select(static field => $"this.{field.Name} = {field.ValidLowerName};"));
        var createAssignments = componentData.Fields.Length == 0 ? string.Empty : string.Join("\n", componentData.Fields.Select(static field => $"component.{field.Name} = {field.ValidLowerName};"));
        var destroyAssignments = componentData.Fields.Length == 0 ? string.Empty : string.Join("\n", componentData.Fields.Select(static field => $"component.{field.Name} = default;"));
        var interfaceUsage = componentData.IndexType == EntityIndexType.Array ? $": I{componentData.Prefix}Indexable" : string.Empty;
        var interfaceDeclaration = componentData.IndexType == EntityIndexType.Array ? $"public interface I{componentData.Prefix}Indexable{{public int GetIndex({methodSignature});}}" : string.Empty;

        return $$"""
                 {{interfaceDeclaration}}
                 public sealed partial class {{componentData.Name}}{{interfaceUsage}}
                 {
                     static readonly System.Collections.Generic.Stack<{{componentData.Name}}> ComponentPool = new ();

                     {{componentData.Name}}({{methodSignature}})
                     {
                         {{ctorAssignments}}
                     }

                     public static {{componentData.Name}} CreateComponent({{methodSignature}})
                     {
                         if (ComponentPool.Count <= 0)
                             return new {{componentData.Name}}({{methodArguments}});

                         var component = ComponentPool.Pop();

                         {{createAssignments}}
                         return component;
                     }
                     public static void DestroyComponent({{componentData.Name}} component)
                     {
                         {{destroyAssignments}}
                         ComponentPool.Push(component);
                     }
                 }
                 """;
    }
}
