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

        // var allComponents = componentDatas
        //     .Select((x, _) => x.Prefix)
        //     .Collect()
        //     .Sort();
        //
        // var allContexts = contextDatas
        //     .Select((x, _) => x.Prefix)
        //     .Collect()
        //     .Sort();

        // var allComponents = sortedComponents.Select((x, _) => x.Select(y => y.Prefix).ToImmutableArray());
        // var allContexts = sortedContextDatas.Select((x, _) => x.Select(y => y.Prefix).ToImmutableArray());

        var ComponentsWithContexts = componentDatas
            .Combine(contextDatas.Collect())
            .Select(CombineComponentsWithContexts);

        var ComponentContextPair = ComponentsWithContexts.SelectMany((x, _) => x.ContextDatas.Select(ContextData => (x.ComponentData, ContextData)));
        //
        // var ContextsWithComponents = contextDatas
        //     .Combine(componentDatas.Collect())
        //     .Select(CombineContextsWithComponents);

        initContext.RegisterSourceOutput(componentDatas, GenerateComponentOutput);
        initContext.RegisterSourceOutput(ComponentContextPair, GenerateEntityExtensionsOutput);
        // initContext.RegisterSourceOutput(groupDatas, GenerateGroupOutput);
        // initContext.RegisterSourceOutput(systemDatas, GenerateSystemOutput);

        initContext.RegisterSourceOutput(contextDatas, GenerateContextOutput);
        initContext.RegisterSourceOutput(contextDatas, GenerateEntityOutput);
        initContext.RegisterSourceOutput(ComponentsWithContexts, GenerateInterfaceExtensionsOutput);

        // initContext.RegisterSourceOutput(allComponents, GenerateComponentsEnumOutput);
        // initContext.RegisterSourceOutput(allContexts, GenerateContextsEnumOutput);
    }

    ComponentWithContexts CombineComponentsWithContexts((ComponentData componentData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
    {
        var contexts = new List<ContextData>();


        foreach (var contextData in data.contextDatas)
        {
            if (data.componentData.ComponentAddedContexts.Contains(contextData.Name))
            {
                contexts.Add(contextData);
                continue;
            }

            if (contextData.Components.Contains(data.componentData.Name))
            {
                contexts.Add(contextData);
                continue;
            }
        }

        return new ComponentWithContexts(data.componentData, contexts.ToImmutableArray());
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

            var methodSignature = data.Fields.Length == 0 ? string.Empty : string.Join(", ", data.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
            var methodArguments = data.Fields.Length == 0 ? string.Empty : string.Join(", ", data.Fields.Select(static field => $"{field.ValidLowerName}"));
            var ctorAssignments = data.Fields.Length == 0 ? string.Empty : string.Join("\n", data.Fields.Select(static field => $"this.{field.Name} = {field.ValidLowerName};"));
            var createAssignments = data.Fields.Length == 0 ? string.Empty : string.Join("\n", data.Fields.Select(static field => $"component.{field.Name} = {field.ValidLowerName};"));
            var destroyAssignments = data.Fields.Length == 0 ? string.Empty : string.Join("\n", data.Fields.Select(static field => $"component.{field.Name} = default;"));

            var content = $$"""
                            public sealed partial class {{data.Name}}
                            {
                                public const string Name = "{{data.Name}}";
                                public const string Added = "{{data.Prefix}}Added";
                                public const string Removed = "{{data.Prefix}}Removed";
                                public const string AddedOrRemoved = "{{data.Prefix}}AddedOrRemoved";

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

    void GenerateEntityExtensionsOutput(SourceProductionContext context, (ComponentData ComponentData, ContextData ContextData) value)
    {
        var componentData = value.ComponentData;
        var contextData = value.ContextData;

        var entityExtensions = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentOutput));

        try
        {
            var methodSignature = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
            var methodSignatureWithLeadingComma = methodSignature == string.Empty ? string.Empty : $", {methodSignature}";
            var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));
            var equalityComparer = componentData.Fields.Length == 0 ? string.Empty : string.Join(" && ", componentData.Fields.Select(field => $"entity.{componentData.Name}.{field.Name} == {field.ValidLowerName}"));


            var entityExtensionsContent =
                $$"""
                  public static class {{contextData.Prefix}}Entity{{componentData.Prefix}}Extensions
                  {
                      public static bool Has{{componentData.Prefix}}(this {{contextData.Prefix}}Entity entity) => entity.{{componentData.Name}} != null;

                      public static {{componentData.Name}} Get{{componentData.Prefix}}(this {{contextData.Prefix}}Entity entity) => entity.{{componentData.Name}};

                      public static {{contextData.Prefix}}Entity Set{{componentData.Prefix}}(this {{contextData.Prefix}}Entity entity{{methodSignatureWithLeadingComma}})
                      {
                          if (!entity.IsEnabled)
                          {
                              return entity;
                          }

                          if (entity.{{componentData.Name}} == null)
                          {
                              entity.{{componentData.Name}} = {{componentData.Name}}.CreateComponent(value);
                              return entity;
                          }

                          if ({{equalityComparer}})
                          {
                              return entity;
                          }

                          var previousComponent = entity.{{componentData.Name}};
                          entity.{{componentData.Name}} = {{componentData.Name}}.CreateComponent({{methodArguments}});

                          {{componentData.Name}}.DestroyComponent(previousComponent);
                          return entity;
                      }

                      public static {{contextData.Prefix}}Entity Remove{{componentData.Prefix}}(this {{contextData.Prefix}}Entity entity)
                      {
                          if (!entity.IsEnabled)
                          {
                              return entity;
                          }

                          if (entity.{{componentData.Name}} == null)
                          {
                              return entity;
                          }

                          var previousComponent = entity.{{componentData.Name}};
                          entity.{{componentData.Name}} = null;

                          {{componentData.Name}}.DestroyComponent(previousComponent);

                          return entity;
                      }
                  }
                  """;

            using (new NamespaceBuilder(entityExtensions, componentData.Namespace))
            {
                entityExtensions.AppendLine(entityExtensionsContent);
            }
        }
        catch (Exception e)
        {
            entityExtensions.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint(componentData.Namespace, $"{contextData.Prefix}Entity{componentData.Prefix}Extensions"), entityExtensions.ToString());
    }

    void GenerateInterfaceExtensionsOutput(SourceProductionContext context, ComponentWithContexts componentWithContexts)
    {
        var data = componentWithContexts.ComponentData;
        var contextDatas = componentWithContexts.ContextDatas;

        var methodSignature = string.Join(", ", data.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
        var methodSignatureWithLeadingComma = methodSignature == string.Empty ? string.Empty : $", {methodSignature}";
        var methodArguments = data.Fields.Length == 0 ? string.Empty : string.Join(", ", data.Fields.Select(static field => $"{field.ValidLowerName}"));
        var methodArgumentsWithLeadingComma = methodArguments == string.Empty ? string.Empty : $", {methodArguments}";

        var interfaceExtensions = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentOutput));

        string hasComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.FullPrefix}Entity{data.Prefix}Extensions.Has{data.Prefix}({x.Prefix}Entity),"));
        string getComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.FullPrefix}Entity{data.Prefix}Extensions.Get{data.Prefix}({x.Prefix}Entity),"));
        string setComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.FullPrefix}Entity{data.Prefix}Extensions.Set{data.Prefix}({x.Prefix}Entity{methodArgumentsWithLeadingComma}),"));
        string removeComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.FullPrefix}Entity{data.Prefix}Extensions.Remove{data.Prefix}({x.Prefix}Entity),"));
        var interfaceExtensionsContent =
            $$"""
                public static class I{{data.Prefix}}Extensions
                {
                    public static bool HasAsset(this I{{data.Prefix}}Entity entity)
                    => entity switch
                    {
                {{hasComponent}}
                        _ => default
                    };
                    public static {{data.Name}} Get{{data.Prefix}}(this I{{data.Prefix}}Entity entity)
                    => entity switch
                    {
                {{getComponent}}
                        _ => default
                    };
                    public static I{{data.Prefix}}Entity Set{{data.Prefix}}(this I{{data.Prefix}}Entity entity{{methodSignatureWithLeadingComma}})
                    => entity switch
                    {
                {{setComponent}}
                        _ => default
                    };
                    public static I{{data.Prefix}}Entity Remove{{data.Prefix}}(this I{{data.Prefix}}Entity entity)
                    => entity switch
                    {
                {{removeComponent}}
                        _ => default
                    };
                }
              """;

        using (new NamespaceBuilder(interfaceExtensions, data.Namespace))
        {
            interfaceExtensions.AppendLine($"public interface I{data.Prefix}Entity : Entitas.IEntity {{ }} \n");
            interfaceExtensions.AppendLine(interfaceExtensionsContent);
        }

        context.AddSource(FileNameHint(data.Namespace, $"I{data.Prefix}EntityExtensions"), interfaceExtensions.ToString());
    }

    void GenerateEntityOutput(SourceProductionContext context, ContextWithComponents value)
    {
        var contextData = value.ContextData;
        var componentDatas = value.ComponentDatas;

        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
                            public class {{contextData.Prefix}}Entity : Entitas.EntityBase
                            {
                                public {{contextData.Name}} Context { get; private set; }

                                private {{contextData.Prefix}}Entity(){}
                                public void OnCreated(int id, {{contextData.Name}} context)
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

            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(content);
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint(contextData.Namespace, $"{contextData.Prefix}Entity"), stringBuilder.ToString());
    }

    void GenerateContextOutput(SourceProductionContext context, ContextData data)
    {
        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
                            public sealed partial class {{data.Name}} : Entitas.ContextBase
                            {
                                public const string Name = "{{data.Name}}";
                                public static readonly Entitas.ContextInfo ContextInfo = new Entitas.ContextInfo("{{data.Name}}",
                                    null,
                                    null);

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
