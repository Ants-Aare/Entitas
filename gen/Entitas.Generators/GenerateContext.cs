using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateContext
{
    public static void GenerateContextOutput(SourceProductionContext context, ContextWithComponents data)
    {
        var contextData = data.ContextData;

        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateContext));

            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(GetContent(data));
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(contextData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, contextData.Name), stringBuilder.ToString());
    }

    static string GetContent(ContextWithComponents data)
    {
        var contextData = data.ContextData;
        var componentDatas = data.ComponentDatas;
        var systemDatas = data.SystemDatas;
        var strings = componentDatas.Length == 0 ? string.Empty : string.Join(", ", componentDatas.Select(static component => $"\"{component.Name}\""));
        var types = componentDatas.Length == 0 ? string.Empty : string.Join(", ", componentDatas.Select(static component => $"typeof({component.FullName})"));
        var systemReferences = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t", systemDatas.Select(static system => $"public {system.FullName} {system.ValidLowerName};"));
        var constructorSignature = systemDatas.Length == 0 ? string.Empty : string.Join(", ", systemDatas.Select(static system => $"{system.FullName} {system.Name}"));
        var constructorBody = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"context.{system.ValidLowerName} = {system.Name};\n\t\t{system.Name}.Enable();"));

        return $$"""
                 public sealed partial class {{contextData.Name}} : Entitas.ContextBase
                 {
                     public const string Name = "{{contextData.Name}}";
                     public static readonly Entitas.ContextInfo ContextInfo = new Entitas.ContextInfo("{{contextData.Name}}",
                         new[]{{{strings}}},
                         new[]{{{types}}});

                     static int _creationIndex;
                     static readonly System.Collections.Generic.Stack<{{contextData.Prefix}}Entity> EntityPool;
                     static readonly System.Collections.Generic.Stack<{{contextData.Name}}> ContextPool;

                     System.Collections.Generic.Dictionary<int, {{contextData.Prefix}}Entity> _enabledEntities = new();
                     {{contextData.Prefix}}Entity contextEntity;
                     {{systemReferences}}

                     private {{contextData.Name}}()
                     {
                         Id = _creationIndex++;
                         IsEnabled = true;
                         contextEntity = CreateEntity();
                     }

                     public static {{contextData.Name}} CreateContext({{constructorSignature}})
                     {
                         var context = new {{contextData.Name}}();
                         {{constructorBody}}
                         return context;
                     }

                     public void DestroyContext()
                     {
                     }

                     public {{contextData.Prefix}}Entity CreateEntity()
                     {
                         var entity = EntityPool.Count <= 0
                             ? new {{contextData.Prefix}}Entity()
                             : EntityPool.Pop();

                         entity.InitializeEntity(_creationIndex++, this);
                         _enabledEntities.Add(_creationIndex, entity);
                         return entity;
                     }

                     internal void ReturnEntity({{contextData.Prefix}}Entity entity)
                     {
                         if (contextEntity == entity)
                             contextEntity = CreateEntity();
                         _enabledEntities.Remove(entity.Id);
                         EntityPool.Push(entity);
                     }
                 }
                 """;
    }
}
