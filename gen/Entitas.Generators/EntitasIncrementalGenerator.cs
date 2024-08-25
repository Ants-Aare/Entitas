using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;
using static Entitas.Generators.Templates;

namespace Entitas.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class EntitasIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // var systemDatas = initContext.SyntaxProvider
        //     .CreateSyntaxProvider(SystemData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<SystemData>)
        //     .RemoveEmptyValues();
        //
        // var groupDatas = initContext.SyntaxProvider
        //     .CreateSyntaxProvider(GroupData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<GroupData>)
        //     .RemoveEmptyValues();

        var componentDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ComponentData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ComponentData>)
            .RemoveEmptyValues();
        var contextDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ContextData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ContextData>)
            .RemoveEmptyValues();

        // var sortedComponents = componentDatas
        //     .Collect()
        //     .Sort();
        // var sortedContextDatas = contextDatas
        //     .Collect()
        //     .Sort();

        // var allComponents = componentDatas
        //     .Select((x, _) => x.Prefix)
        //     .Collect()
        //     .Sort();
        //
        // var allContexts = contextDatas
        //     .Select((x, _) => x.Prefix)
        //     .Collect()
        //     .Sort();

        // var allComponents = sortedComponents.Select((x, _) => x.Select(y => y.Prefix).ToList());
        // var allContexts = contextDatas.Select((x, _) => x.Select(y => y.Prefix).ToImmutableArray());

        // var ComponentsWithContexts = sortedComponents.SelectManyWithIndex(ComponentData.SetIndex)
        //     .Combine(sortedContextDatas)
        //     .Select(CombineComponentsWithContexts);

        // var ContextsWithComponents = sortedContextDatas.SelectManyWithIndex(ContextData.SetIndex)
        //     .Combine(sortedComponents)
        //     .Select(CombineContextsWithComponents);

        // initContext.RegisterSourceOutput(ComponentsWithContexts, GenerateInterfaceExtensionsOutput);
        initContext.RegisterSourceOutput(componentDatas, GenerateComponentOutput);
        // initContext.RegisterSourceOutput(ComponentsWithContexts, GenerateEntityExtensionsOutput);
        // initContext.RegisterSourceOutput(groupDatas, GenerateGroupOutput);
        // initContext.RegisterSourceOutput(systemDatas, GenerateSystemOutput);

        initContext.RegisterSourceOutput(contextDatas, GenerateContextOutput);
        initContext.RegisterSourceOutput(contextDatas, GenerateEntityOutput);


        // initContext.RegisterSourceOutput(allComponents, GenerateComponentsEnumOutput);
        // initContext.RegisterSourceOutput(allContexts, GenerateContextsEnumOutput);
    }

    ComponentWithContexts CombineComponentsWithContexts((ComponentData componentData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
    {
        var contexts = new List<ContextData>();
        foreach (var contextData in data.contextDatas)
        {
            if (data.componentData.ComponentAddedContexts.Contains(contextData.Index))
            {
                contexts.Add(contextData);
                continue;
            }

            if (contextData.Components.Contains(data.componentData.Index))
            {
                contexts.Add(contextData);
            }
        }

        return new ComponentWithContexts(data.componentData, contexts);
    }

    ContextWithComponents CombineContextsWithComponents((ContextData contextData, ImmutableArray<ComponentData> componentDatas) data, CancellationToken ct)
    {
        return default;
    }

    void GenerateContextsEnumOutput(SourceProductionContext context, ImmutableArray<string> contextNames)
    {
        var enumsStringBuilder = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentsEnumOutput));

        using (new NamespaceBuilder(enumsStringBuilder, "Entitas.Generated"))
        {
            enumsStringBuilder.AppendLine("    public enum Contexts\n    {");
            foreach (var contextName in contextNames)
            {
                enumsStringBuilder.AppendLine($"        {contextName},");
            }

            enumsStringBuilder.AppendLine("    }");
        }

        context.AddSource(FileNameHint("Entitas.Generated", "ComponentEnums"), enumsStringBuilder.ToString());
    }

    void GenerateComponentsEnumOutput(SourceProductionContext context, ImmutableArray<string> componentNames)
    {
        var enumsStringBuilder = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentsEnumOutput));
        try
        {
            using (new NamespaceBuilder(enumsStringBuilder, "Entitas.Generated"))
            {
                enumsStringBuilder.AppendLine("    public enum Components\n    {");
                foreach (var componentData in componentNames)
                {
                    enumsStringBuilder.AppendLine($"        {componentData},");
                }

                enumsStringBuilder.AppendLine("    }\n \n");


                enumsStringBuilder.AppendLine("    public enum ComponentEvents\n{");
                foreach (var componentData in componentNames)
                {
                    enumsStringBuilder.AppendLine($"        {componentData}Added,\n        {componentData}Removed,\n        {componentData}AddedOrRemoved,\n");
                }

                enumsStringBuilder.AppendLine("    }");
            }
        }
        catch (Exception e)
        {
            enumsStringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint("Entitas.Generated", "ComponentEnums"), enumsStringBuilder.ToString());
    }

    void GenerateGroupOutput(SourceProductionContext context, GroupData data) { }

    void GenerateSystemOutput(SourceProductionContext context, SystemData data) { }

    void GenerateComponentOutput(SourceProductionContext context, ComponentData data)
    {
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var methodSignature = data.Fields.Count == 0 ? string.Empty : string.Join(", ", data.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
            var methodArguments = data.Fields.Count == 0 ? string.Empty : string.Join(", ", data.Fields.Select(static field => $"{field.ValidLowerName}"));
            var ctorAssignments = data.Fields.Count == 0 ? string.Empty : string.Join("\n", data.Fields.Select(static field => $"{field.Name} = {field.ValidLowerName};"));
            var createAssignments = data.Fields.Count == 0 ? string.Empty : string.Join("\n", data.Fields.Select(static field => $"component.{field.Name} = {field.ValidLowerName};"));
            var destroyAssignments = data.Fields.Count == 0 ? string.Empty : string.Join("\n", data.Fields.Select(static field => $"component.{field.Name} = default;"));

            var content = $$"""
                            public sealed partial class {{data.Name}}
                            {
                                static readonly System.Collections.Generic.Stack<{{data.Name}}> ComponentPool = new ();

                                {{data.Name}}({{methodSignature}})
                                {
                                    {{ctorAssignments}}
                                }

                                public static {{data.Name}} CreateComponent({{methodSignature}})
                                {
                                    if (ComponentPool.Count <= 0)
                                        return new {{data.Name}}({{methodArguments}});

                                    var component = ComponentPool.Pop();

                                    {{createAssignments}}
                                    return component;
                                }
                                public static void DestroyComponent({{data.Name}} component)
                                {
                                    {{destroyAssignments}}
                                    ComponentPool.Push(component);
                                }
                            }
                            """;

            stringBuilder.AppendGenerationWarning(nameof(GenerateComponentOutput));

            using (new NamespaceBuilder(stringBuilder, data.Namespace))
            {
                stringBuilder.AppendLine(content);
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(data.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint(data.Namespace, data.Name), stringBuilder.ToString());
    }

    void GenerateEntityExtensionsOutput(SourceProductionContext context, ComponentWithContexts componentWithContexts)
    {
        var data = componentWithContexts.ComponentData;
        foreach (var contextData in componentWithContexts.ContextDatas)
        {
            var entityExtensions = new StringBuilder()
                .AppendGenerationWarning(nameof(GenerateComponentOutput));

            try
            {
                var methodSignature = data.Fields.Count == 0 ? string.Empty : string.Join(", ", data.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
                var methodSignatureWithLeadingComma = methodSignature == string.Empty ? string.Empty : $", {methodSignature}";
                var methodArguments = data.Fields.Count == 0 ? string.Empty : string.Join(", ", data.Fields.Select(static field => $"{field.ValidLowerName}"));
                var equalityComparer = data.Fields.Count == 0 ? string.Empty : string.Join(" && ", data.Fields.Select(field => $"entity.{data.Name}.{field.Name} == {field.ValidLowerName}"));


                var entityExtensionsContent =
                    $$"""
                      public static class {{contextData.Prefix}}Entity{{data.Prefix}}Extensions
                      {
                          public static bool Has{{data.Prefix}}(this {{contextData.Prefix}}Entity entity) => entity.{{data.Name}} != null;

                          public static {{data.Name}} Get{{data.Prefix}}(this {{contextData.Prefix}}Entity entity) => entity.{{data.Name}};

                          public static {{contextData.Prefix}}Entity Set{{data.Prefix}}(this {{contextData.Prefix}}Entity entity{{methodSignatureWithLeadingComma}})
                          {
                              if (!entity.IsEnabled)
                              {
                                  return entity;
                              }

                              if (entity.{{data.Name}} == null)
                              {
                                  entity.{{data.Name}} = {{data.Name}}.CreateComponent(value);
                                  return entity;
                              }

                              if ({{equalityComparer}})
                              {
                                  return entity;
                              }

                              var previousComponent = entity.{{data.Name}};
                              entity.{{data.Name}} = {{data.Name}}.CreateComponent({{methodArguments}});

                              {{data.Name}}.DestroyComponent(previousComponent);
                              return entity;
                          }

                          public static {{contextData.Prefix}}Entity Remove{{data.Prefix}}(this {{contextData.Prefix}}Entity entity)
                          {
                              if (!entity.IsEnabled)
                              {
                                  return entity;
                              }

                              if (entity.{{data.Name}} == null)
                              {
                                  return entity;
                              }

                              var previousComponent = entity.{{data.Name}};
                              entity.{{data.Name}} = null;

                              {{data.Name}}.DestroyComponent(previousComponent);

                              return entity;
                          }
                      }
                      """;

                using (new NamespaceBuilder(entityExtensions, data.Namespace))
                {
                    entityExtensions.AppendLine(entityExtensionsContent);
                }
            }
            catch (Exception e)
            {
                entityExtensions.AppendLine(e.ToString());
            }

            context.AddSource(FileNameHint(data.Namespace, $"{contextData.Prefix}Entity{data.Prefix}Extensions"), entityExtensions.ToString());
        }
    }

    void GenerateInterfaceExtensionsOutput(SourceProductionContext context, ComponentWithContexts componentWithContexts)
    {
        var data = componentWithContexts.ComponentData;
        var methodSignature = string.Join(", ", data.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
        var methodSignatureWithLeadingComma = methodSignature == string.Empty ? string.Empty : $", {methodSignature}";

        var interfaceExtensions = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentOutput));

        var interfaceExtensionsContent =
            $$"""
              public static class I{{data.Prefix}}Extensions
              {
                  public static bool Has{{data.Prefix}}(this I{{data.Prefix}}Entity entity)
                  {
                      return true;
                  }
                  public static {{data.Name}} Get{{data.Prefix}}(this I{{data.Prefix}}Entity entity)
                  {
                      return default;
                  }
                  public static I{{data.Prefix}}Entity Set{{data.Prefix}}(this I{{data.Prefix}}Entity entity{{methodSignatureWithLeadingComma}})
                  {
                      return entity;
                  }
                  public static I{{data.Prefix}}Entity Remove{{data.Prefix}}(this I{{data.Prefix}}Entity entity)
                  {
                      return entity;
                  }
              }
              """;

        using (new NamespaceBuilder(interfaceExtensions, data.Namespace))
        {
            interfaceExtensions.AppendLine($"public interface I{data.Prefix}Entity : Entitas.IEntity {{ }} \n");
            interfaceExtensions.AppendLine(interfaceExtensionsContent);
        }

        context.AddSource(FileNameHint(data.Namespace, $"I{data.Prefix}EntityExtensions"), interfaceExtensions.ToString());
    }

    void GenerateEntityOutput(SourceProductionContext context, ContextData data)
    {
        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
                            public class {{data.Prefix}}Entity : Entitas.EntityBase
                            {
                                public {{data.Name}} Context { get; private set; }

                                internal {{data.Prefix}}Entity(){}
                                public void OnCreated(int id, {{data.Name}} context)
                                {
                                    Id = id;
                                    Context = context;
                                    IsEnabled = true;
                                }
                                public void DestroyImmediate()
                                {
                                    Context.ReturnEntity(this);

                                    Context = null;
                                    IsEnabled = false;
                                    Id = -1;
                                }
                            }
                            """;

            stringBuilder.AppendGenerationWarning(nameof(GenerateContextOutput));

            using (new NamespaceBuilder(stringBuilder, data.Namespace))
            {
                stringBuilder.AppendLine(content);
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint(data.Namespace, $"{data.Prefix}Entity"), stringBuilder.ToString());
    }

    void GenerateContextOutput(SourceProductionContext context, ContextData data)
    {
        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
                            public sealed partial class {{data.Name}} : Entitas.ContextBase
                            {
                                public static readonly Entitas.ContextInfo ContextInfo = new Entitas.ContextInfo("{{data.Name}}",
                                    new[] { },
                                    new[] { });

                                static int _creationIndex;
                                static readonly System.Collections.Generic.Stack<{{data.Prefix}}Entity> EntityPool;
                                static readonly System.Collections.Generic.Stack<{{data.Name}}> ContextPool;

                                System.Collections.Generic.Dictionary<int, {{data.Prefix}}Entity> _enabledEntities = new();
                                internal {{data.Prefix}}Entity contextEntity;

                                private {{data.Name}}()
                                {
                                    Id = _creationIndex++;
                                    IsEnabled = true;
                                    contextEntity = CreateEntity();
                                }

                                public static {{data.Name}} CreateContext()
                                {
                                    var context = new {{data.Name}}()
                                    {
                                    };

                                    return context;
                                }

                                public void DestroyContext()
                                {
                                }

                                public {{data.Prefix}}Entity CreateEntity()
                                {
                                    var entity = EntityPool.Count <= 0
                                        ? new {{data.Prefix}}Entity()
                                        : EntityPool.Pop();

                                    entity.OnCreated(_creationIndex++, this);
                                    _enabledEntities.Add(_creationIndex, entity);
                                    return entity;
                                }

                                internal void ReturnEntity({{data.Prefix}}Entity entity)
                                {
                                    _enabledEntities.Remove(entity.Id);
                                    EntityPool.Push(entity);
                                }
                            }
                            """;

            stringBuilder.AppendGenerationWarning(nameof(GenerateContextOutput));

            using (new NamespaceBuilder(stringBuilder, data.Namespace))
            {
                stringBuilder.AppendLine(content);
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(data.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint(data.Namespace, data.Name), stringBuilder.ToString());
    }
}
