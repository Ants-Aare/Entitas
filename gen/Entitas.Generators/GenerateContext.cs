using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateContext
{
    public static void GenerateContextOutput(SourceProductionContext context, ExtendedContextData data)
    {
        var contextData = data.ContextData;

        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateContext));

            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(GetContent(data));
                if(data.GroupDatas.Length != 0)
                    stringBuilder.AppendLine(GetGroupContent(data));
                stringBuilder.Append('}');
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(contextData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, contextData.Name), stringBuilder.ToString());
    }

    static string GetGroupContent(ExtendedContextData data)
    {
        // var contextData = data.ContextData;
        // var groupDatas = data.GroupDatas;

        return $$"""

                 """;
    }

    static string GetContent(ExtendedContextData data)
    {
        var contextData = data.ContextData;
        var componentDatas = data.ComponentDatas;
        var systemDatas = data.SystemDatas;
        var groupDatas = data.GroupDatas;
        var strings = componentDatas.Length == 0 ? "null" : $"new[]{{{string.Join(", ", componentDatas.Select(static component => $"\"{component.Name}\""))}}}";
        var types = componentDatas.Length == 0 ? "null" : $"new[]{{{string.Join(", ", componentDatas.Select(static component => $"typeof({component.FullName})"))}}}";
        var systemReferences = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t", systemDatas.Select(static system => $"public {system.FullName} {system.ValidLowerName};"));
        var constructorSignature = systemDatas.Length == 0 ? string.Empty : string.Join(", ", systemDatas.Select(static system => $"{system.FullName} {system.Name}"));
        var constructorBody = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"context.{system.ValidLowerName} = {system.Name};\n\t\t{system.Name}.Context = context;\n\t\t{system.Name}.Enable();\n"));
        var destroySystems = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"{system.ValidLowerName}.Disable();\n\t\t{system.ValidLowerName} = null;"));
        var disableSystems = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"{system.ValidLowerName}.Disable();"));
        var enableSystems = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"{system.ValidLowerName}.Enable();"));

        var uniqueComponents = data.ComponentDatas.Where(x => x.IsUnique).ToList();
        var destroyUnique = uniqueComponents.Count == 0 ? string.Empty : string.Join("\n\t\t", uniqueComponents.Select(static x => $"{x.FullName}.DestroyComponent({x.Name});"));
        var groupDeclarations = string.Join("\n\t", groupDatas.Select( group => $"public System.Collections.Generic.IEnumerable<{contextData.Prefix}Entity> {group.Name} => {group.ValidLowerName}Dictionary.Values;\n\t[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]\n\tpublic readonly System.Collections.Generic.Dictionary<int, {contextData.Prefix}Entity> {group.ValidLowerName}Dictionary = new();"));

        var systems = systemDatas.Where(x => x.NeedsCustomInterface()).ToList();
        var systemInterfaces = systems.Count == 0 ? string.Empty : ',' + string.Join(", ", systems.Select(static x => $"{x.Namespace.NamespaceClassifier()}I{x.Name}Context"));

        var featureInterfaces = contextData.Features.Length == 0 ? string.Empty : ',' + string.Join(", ", contextData.Features.Select(static x => $"{x.NamespaceSpecifier}I{x.Name}Context"));

        return $$"""
                 public sealed partial class {{contextData.Name}} : Entitas.ContextBase{{systemInterfaces}}{{featureInterfaces}}
                 {
                     public const string Name = "{{contextData.Name}}";
                     public static readonly Entitas.ContextInfo ContextInfo = new Entitas.ContextInfo("{{contextData.Name}}",
                         {{strings}},
                         {{types}});

                     static int _creationIndex;
                     static readonly System.Collections.Generic.Stack<{{contextData.Prefix}}Entity> EntityPool;
                     static readonly System.Collections.Generic.Stack<{{contextData.Name}}> ContextPool;

                     System.Collections.Generic.Dictionary<int, {{contextData.Prefix}}Entity> _enabledEntities = new();
                     {{contextData.Prefix}}Entity contextEntity;
                     {{systemReferences}}
                     {{groupDeclarations}}

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
                         contextEntity.DestroyImmediate();
                         foreach (var (_,entity) in _enabledEntities)
                         {
                             entity.DestroyImmediate();
                         }
                         {{destroySystems}}
                         {{destroyUnique}}
                         IsEnabled = false;
                         ContextPool.Push(this);
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

                     [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                     internal void ReturnEntity({{contextData.Prefix}}Entity entity)
                     {
                         if (contextEntity == entity)
                             contextEntity = CreateEntity();
                         _enabledEntities.Remove(entity.Id);
                         EntityPool.Push(entity);
                     }

                     public void EnableSystems()
                     {
                      {{enableSystems}}
                     }
                     public void DisableSystems()
                     {
                      {{disableSystems}}
                     }
                     ~{{contextData.Name}}()
                     {
                         DestroyContext();
                     }
                 """;
    }
}
