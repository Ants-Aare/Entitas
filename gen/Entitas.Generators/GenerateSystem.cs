using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateSystem
{
    public static void GenerateSystemOutput(SourceProductionContext context, ExtendedSystemData data)
    {
        var systemData = data.SystemData;
        var componentDatas = data.ComponentDatas;
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var entityType = data.GetEntityType();
            var contextType = data.GetContextType();

            stringBuilder.AppendGenerationWarning(nameof(GenerateSystem));
            using (new NamespaceBuilder(stringBuilder, systemData.Namespace))
            {
                stringBuilder.AppendLine(GetClassContent(data));

                if (systemData.IsReactiveSystem)
                    stringBuilder.AppendLine(GetReactiveContent(entityType));
                if (systemData.IsExecuteSystem)
                    stringBuilder.AppendLine(GetExecuteContent(contextType));
                // if (systemData.IsInitializeSystem)
                //     stringBuilder.AppendLine(GetInitializeContent(entityType));

                stringBuilder.Append('}');
                if (systemData.HasMultipleConstraints())
                {
                    var entities = string.Join(", ", componentDatas.Select(x => $"{x.Namespace.NamespaceClassifier()}I{x.Prefix}Entity"));
                    var contexts = string.Join(", ", componentDatas.Select(x => $"{x.Namespace.NamespaceClassifier()}I{x.Prefix}Context"));
                    stringBuilder.AppendLine($"public interface I{systemData.Name}Entity : {entities}{{ }} \n public interface I{systemData.Name}Context : {contexts}{{ }}");
                }
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(systemData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(systemData.Namespace, systemData.Name), stringBuilder.ToString());
    }

    static string GetClassContent(ExtendedSystemData data)
    {
        var systemData = data.SystemData;
        var entityType = data.GetEntityType();
        var contextType = data.GetContextType();

        var interfaces = new StringBuilder();
        if (systemData.IsReactiveSystem)
            interfaces.Append($"Entitas.IReactiveSystem<{entityType}>");
        if (systemData.IsExecuteSystem)
        {
            if (systemData.IsReactiveSystem)
                interfaces.Append(',');
            interfaces.Append(string.IsNullOrEmpty(contextType) ? "Entitas.IExecuteSystem" : $"Entitas.IExecuteSystem<{contextType}>");
        }
        if (systemData.IsInitializeSystem)
        {
            if (systemData.IsReactiveSystem || systemData.IsExecuteSystem)
                interfaces.Append(',');
            interfaces.Append(string.IsNullOrEmpty(contextType) ? "Entitas.IInitializeSystem" : $"Entitas.IInitializeSystem<{contextType}>");
        }

        var contextDeclaration = string.IsNullOrEmpty(contextType) ? string.Empty : $"public {contextType} Context;";

        return $$"""
                 public sealed partial class {{systemData.Name}} : {{interfaces}}
                 {
                     static readonly System.Collections.Generic.HashSet<{{systemData.Name}}> Instances = new();

                     public void Enable() => Instances.Add(this);

                     public void Disable() => Instances.Remove(this);

                     ~{{systemData.Name}}()=> Instances.Remove(this);

                     {{contextDeclaration}}

                 """;
    }

    static string GetReactiveContent(string entityType)
    {
        return $$"""
                     readonly System.Collections.Generic.HashSet<{{entityType}}> _collector = new ();
                     readonly System.Collections.Generic.List<{{entityType}}> _buffer = new ();
                     public static void UpdateReactiveSystems()
                     {
                         foreach (var systemInstance in Instances)
                             systemInstance.UpdateReactiveSystem();
                     }
                     public void UpdateReactiveSystem()
                     {
                         if (_collector.Count == 0)
                             return;

                         foreach (var entity in _collector)
                             if (Filter(entity))
                                 _buffer.Add(entity);

                         _collector.Clear();

                         if (_buffer.Count == 0)
                             return;

                         try
                         {
                             Execute(_buffer);
                         }
                         finally
                         {
                             _buffer.Clear();
                         }
                     }

                     public void OnEntityTriggered({{entityType}} entity)
                     {
                         _collector.Add(entity);
                     }
                 """;
    }

    static string GetExecuteContent(string contextType)
    {
        return $$"""
                     public static void UpdateExecuteSystems()
                     {
                         foreach (var systemInstance in Instances)
                             systemInstance.UpdateExecuteSystem();
                     }
                     public void UpdateExecuteSystem()
                     {
                         try
                         {
                             Execute({{(string.IsNullOrEmpty(contextType) ? string.Empty : "Context")}});
                         }
                         finally
                         {
                         }
                     }

                     public void OnEntityTriggered({{contextType}} entity)
                     {
                         _collector.Add(entity);
                     }
                 """;
    }
}
