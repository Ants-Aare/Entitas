using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

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
                stringBuilder.AppendLine($"public interface I{componentData.Prefix}Context : Entitas.IContext {{ }} \npublic interface I{componentData.Prefix}Entity : Entitas.IEntity {{ }} \n");
                stringBuilder.AppendLine(GetContent(componentData));
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(componentData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(StringUtility.FileNameHint(componentData.Namespace, componentData.Name), stringBuilder.ToString());
    }

    static string GetContent(ComponentData componentData)
    {
        var methodSignature = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
        var methodSignatureLeadingComma = componentData.GetMethodSignatureLeadingComma();
        var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));
        var ctorAssignments = componentData.Fields.Length == 0 ? string.Empty : string.Join("\n", componentData.Fields.Select(static field => $"this.{field.Name} = {field.ValidLowerName};"));
        var createAssignments = componentData.Fields.Length == 0 ? string.Empty : string.Join("\n", componentData.Fields.Select(static field => $"component.{field.Name} = {field.ValidLowerName};"));
        var destroyAssignments = componentData.Fields.Length == 0 ? string.Empty : string.Join("\n", componentData.Fields.Select(static field => $"component.{field.Name} = default;"));
        var interfaceUsage = componentData.IndexType == EntityIndexType.Array ? $": I{componentData.Prefix}Indexable" : string.Empty;
        var interfaceDeclarations = new StringBuilder();

        if (componentData.IndexType == EntityIndexType.Array)
            interfaceDeclarations.AppendLine($"public interface I{componentData.Prefix}Indexable{{public int GetIndex({methodSignature});}}");
        if (componentData.HasEvents)
        {
            foreach (var eventData in componentData.Events)
            {
                interfaceDeclarations.Append("public interface I")
                    .Append(componentData.Prefix)
                    .Append(eventData.ComponentEvent)
                    .Append("Listener{public void On")
                    .Append(componentData.Prefix)
                    .Append(eventData.ComponentEvent)
                    .Append("(I")
                    .Append(componentData.Prefix)
                    .Append(eventData.ListenTarget == ListenTarget.Entity ? "Entity entity" : "Context context");
                if (eventData.ComponentEvent != ComponentEvent.Removed)
                    interfaceDeclarations.Append(methodSignatureLeadingComma);
                interfaceDeclarations.Append(");");
                interfaceDeclarations.Append("public void StartListeningTo").Append(componentData.Prefix).Append(eventData.ComponentEvent).Append("(I").Append(componentData.TypeData.Prefix).Append("Entity entity);")
                    .Append("public void StopListeningTo").Append(componentData.Prefix).Append(eventData.ComponentEvent).Append("(I").Append(componentData.TypeData.Prefix).Append("Entity entity);");
                interfaceDeclarations.Append("}\n");
            }
        }

        var implicitTarget = componentData.Fields.Where(x => !x.IsTypeAnInterface).ToList();
        var implicitOperator = implicitTarget.Count == 0
            ? $"public static implicit operator bool({componentData.Name} component) => component != null;"
            : $"public static implicit operator {implicitTarget[0].TypeName}({componentData.Name} component) => component.{implicitTarget[0].Name};";

        var cleanupContent = componentData.IsCleanup ? GetCleanupContent(componentData) : string.Empty;

        return $$"""
                 {{interfaceDeclarations}}
                 public sealed partial class {{componentData.Name}}{{interfaceUsage}}
                 {
                     static readonly System.Collections.Generic.Stack<{{componentData.Name}}> ComponentPool = new ();

                     {{componentData.Name}}({{methodSignature}})
                     {
                         {{ctorAssignments}}
                     }

                     [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                     public static {{componentData.Name}} CreateComponent({{methodSignature}})
                     {
                         if (ComponentPool.Count <= 0)
                             return new {{componentData.Name}}({{methodArguments}});

                         var component = ComponentPool.Pop();

                         {{createAssignments}}
                         return component;
                     }
                     [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                     public static void DestroyComponent({{componentData.Name}} component)
                     {
                        if(component == null)
                            return;
                         {{destroyAssignments}}
                         ComponentPool.Push(component);
                     }
                     {{cleanupContent}}
                     {{implicitOperator}}
                 }
                 """;
    }

    static string GetCleanupContent(ComponentData componentData)
    {
        return componentData.IsUnique
            ? $$"""
                    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                    public static readonly System.Collections.Generic.HashSet<I{{componentData.Prefix}}Context> Collector = new ();
                    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                    public static void Cleanup()
                    {
                        if (Collector.Count == 0)
                            return;

                        foreach (var context in Collector)
                            context.Remove{{componentData.Prefix}}();
                        Collector.Clear();
                    }
                """
            : $$"""

                    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                    public static readonly System.Collections.Generic.HashSet<I{{componentData.Prefix}}Entity> Collector = new ();
                    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                    public static void Cleanup()
                    {
                        if (Collector.Count == 0)
                            return;

                        foreach (var entity in Collector)
                            {{(componentData.CleanupMode == CleanupMode.RemoveComponent ? $"entity.Remove{componentData.Prefix}();" : "entity.DestroyImmediate();")}}
                        Collector.Clear();
                    }
                """;
    }
}
