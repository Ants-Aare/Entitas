using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

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
                if (data.ContextData.IsUnique)
                    stringBuilder.AppendLine(GetUniqueContent(data));
                else
                    stringBuilder.AppendLine(GetInstancesContent(data));
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

        context.AddSource(StringUtility.FileNameHint(contextData.Namespace, contextData.Name), stringBuilder.ToString());
    }

    static string GetUniqueContent(ExtendedContextData data)
    {
        return $$"""

                 static {{data.ContextData.Name}} Instance;
                 public static {{data.ContextData.Name}} GetInstance() => Instance;
                 [System.Obsolete]
                 public static {{data.ContextData.Name}} GetInstance(int index) => Instance;
                 """;
    }
    static string GetInstancesContent(ExtendedContextData data)
    {
        return $$"""

                 static System.Collections.Generic.List<{{data.ContextData.Name}}> Instances = new();
                 public static {{data.ContextData.Name}} GetInstance() => Instances.Count > 0 ? Instances[0] : null;
                 public static {{data.ContextData.Name}} GetInstance(int index) => index < Instances.Count ? Instances[index] : null;
                 """;
    }


    static string GetContent(ExtendedContextData data)
    {
        var contextData = data.ContextData;
        var componentDatas = data.ComponentDatas;
        var systemDatas = data.SystemDatas;
        var groupDatas = data.GroupDatas;
        var constructorArguments = systemDatas.SelectMany(x => x.ConstructorArguments).Distinct(FieldData.TypeAndNameComparer).ToList();
        var initializeSystemDatas = systemDatas.Where(x => x.IsInitializeSystem).ToList();
        initializeSystemDatas.Sort((a, b) => a.InitializeOrder.CompareTo(b.InitializeOrder));
        var teardownSystemDatas = systemDatas.Where(x => x.IsTeardownSystem).ToList();
        teardownSystemDatas.Sort((a, b) => a.TeardownOrder.CompareTo(b.TeardownOrder));
        var strings = componentDatas.Length == 0 ? "null" : $"new[]{{{string.Join(", ", componentDatas.Select(static component => $"\"{component.Name}\""))}}}";
        var types = componentDatas.Length == 0 ? "null" : $"new[]{{{string.Join(", ", componentDatas.Select(static component => $"typeof({component.FullName})"))}}}";
        var systemReferences = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t", systemDatas.Select(static system => $"public {system.FullName} {system.ValidLowerName};"));
        var constructorSignature = constructorArguments.Count == 0 ? string.Empty : string.Join(", ", constructorArguments.Select(static field => $"{field.TypeName} _{field.Name}"));
        var constructorBody = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"context.{system.ValidLowerName} = new {system.FullName}({system.GetConstructor()}){{Context = context}};\n\t\tcontext.{system.ValidLowerName}.Enable();\n"));
        var destroySystems = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"{system.ValidLowerName}.Disable();\n\t\t{system.ValidLowerName} = null;"));
        var disableSystems = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"{system.ValidLowerName}.Disable();"));
        var enableSystems = systemDatas.Length == 0 ? string.Empty : string.Join("\n\t\t", systemDatas.Select(static system => $"{system.ValidLowerName}.Enable();"));
        var initializeSystems = initializeSystemDatas.Count == 0 ? string.Empty : string.Join("\n\t\t", initializeSystemDatas.Select(static system => $"context.{system.ValidLowerName}.Initialize(); // Order: {system.InitializeOrder}"));
        var teardownSystems = teardownSystemDatas.Count == 0 ? string.Empty : string.Join("\n\t\t", teardownSystemDatas.Select(static system => $"{system.ValidLowerName}.Teardown(); // Order: {system.TeardownOrder}"));

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
                     {{{(contextData.IsUnique ? "\n\t\tif (Instance != null) return Instance;" : String.Empty)}}
                         var context = new {{contextData.Name}}();
                         {{constructorBody}}
                         {{initializeSystems}}
                         {{(contextData.IsUnique ? "Instance = context;" : "Instances.Add(context);")}}
                         return context;
                     }

                     public void DestroyContext()
                     {
                         {{teardownSystems}}
                         contextEntity.DestroyImmediate();
                         foreach (var (_,entity) in _enabledEntities)
                         {
                             entity.DestroyImmediate();
                         }
                         {{destroySystems}}
                         {{destroyUnique}}
                         IsEnabled = false;
                         ContextPool.Push(this);
                         {{(contextData.IsUnique ? "Instance = null;" : "Instances.Remove(this);")}}
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
