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
        //
        // var groupDatas = initContext.SyntaxProvider
        //     .CreateSyntaxProvider(GroupData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<GroupData>)
        //     .RemoveEmptyValues();

        // initContext.RegisterSourceOutput(groupDatas, GenerateGroupOutput);

        var systemDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(SystemData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<SystemData>)
            .RemoveEmptyValues();
        var componentDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ComponentData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ComponentData>)
            .RemoveEmptyValues();
        var contextDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ContextData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ContextData>)
            .RemoveEmptyValues();

        var componentsWithContexts = componentDatas
            .Combine(contextDatas.Collect())
            .Select(CombineComponentsWithContexts);
        var contextsWithComponents = contextDatas
            .Combine(componentDatas.Collect())
            .Select(CombineContextsWithComponents);
        var systemsWithComponents = systemDatas
            .Combine(componentDatas.Collect())
            .Select(CombineSystemsWithComponents);

        var componentContextPair = componentsWithContexts.SelectMany((x, _) => x.ContextDatas.Select(contextData => (x.ComponentData, ContextData: contextData)));

        initContext.RegisterSourceOutput(componentDatas, GenerateComponentOutput);
        initContext.RegisterSourceOutput(componentContextPair, GenerateEntityExtensionsOutput);
        initContext.RegisterSourceOutput(componentContextPair, GenerateContextExtensionsOutput);
        initContext.RegisterSourceOutput(systemsWithComponents, GenerateSystemOutput);

        initContext.RegisterSourceOutput(contextsWithComponents, GenerateContextOutput);
        initContext.RegisterSourceOutput(contextsWithComponents, GenerateEntityOutput);
        initContext.RegisterSourceOutput(componentsWithContexts, GenerateInterfaceExtensionsOutput);
    }

    static ComponentWithContexts CombineComponentsWithContexts((ComponentData componentData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
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
            }
        }

        return new ComponentWithContexts(data.componentData, contexts.ToImmutableArray());
    }

    static SystemWithComponents CombineSystemsWithComponents((SystemData systemData, ImmutableArray<ComponentData> componentDatas) data, CancellationToken ct)
    {
        var componentDatas = new List<ComponentData>();

        foreach (var componentData in data.componentDatas)
        {
            if (data.systemData.TriggeredBy.Select(x => x.component).Contains(componentData.Name))
            {
                componentDatas.Add(componentData);
            }

            if (data.systemData.EntityIs.Contains(componentData.Name))
            {
                componentDatas.Add(componentData);
            }
        }

        return new SystemWithComponents(data.systemData, componentDatas.ToImmutableArray());
    }

    static ContextWithComponents CombineContextsWithComponents((ContextData contextData, ImmutableArray<ComponentData> componentDatas) data, CancellationToken ct)
    {
        var contexts = new List<ComponentData>();


        foreach (var componentData in data.componentDatas)
        {
            if (data.contextData.Components.Contains(componentData.Name))
            {
                contexts.Add(componentData);
            }

            if (componentData.ComponentAddedContexts.Contains(data.contextData.Name))
            {
                contexts.Add(componentData);
            }
        }

        return new ContextWithComponents(data.contextData, contexts.ToImmutableArray());
    }

    // static void GenerateGroupOutput(SourceProductionContext context, GroupData data) { }
    //
    static void GenerateSystemOutput(SourceProductionContext context, SystemWithComponents data)
    {
        var systemData = data.SystemData;
        var componentDatas = data.ComponentDatas;
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var hasMultipleConstraints = systemData.TriggeredBy.Length + systemData.EntityIs.Length > 1;

            string entityType;
            if (hasMultipleConstraints)
            {
                entityType = $"I{systemData.Name}Entity";
            }
            else
            {
                var componentData = componentDatas.First(x => x.Name == systemData.TriggeredBy[0].component);
                entityType = $"{componentData.Namespace.NamespaceClassifier()}I{componentData.Prefix}Entity";
            }

            var content = $$"""
                            public sealed partial class {{systemData.Name}} : Entitas.IReactiveSystem<{{entityType}}>
                            {
                                static readonly System.Collections.Generic.HashSet<{{systemData.Name}}> Instances = new();
                                readonly System.Collections.Generic.List<{{entityType}}> _collector = new ();
                                readonly System.Collections.Generic.List<{{entityType}}> _buffer = new ();
                                public static void UpdateSystems()
                                {
                                    foreach (var systemInstance in Instances)
                                        systemInstance.UpdateSystem();
                                }
                                public void UpdateSystem()
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

                                public void Enable() =>Instances.Add(this);

                                public void Disable() =>Instances.Remove(this);

                                ~{{systemData.Name}}()=>Instances.Remove(this);
                            }
                            """;


            stringBuilder.AppendGenerationWarning(nameof(GenerateComponentOutput));
            using (new NamespaceBuilder(stringBuilder, systemData.Namespace))
            {
                stringBuilder.AppendLine(content);

                if (hasMultipleConstraints)
                {
                    var enumerable = componentDatas.Select(x => $"{x.Namespace.NamespaceClassifier()}I{x.Prefix}Entity");
                    // var enumerable = systemData.TriggeredBy.Select(x => x.component)
                    //     .Concat(systemData.EntityIs)
                    //     .Select(x=> $"I{x}Entity");
                    var interfaceDefinition = $"public interface I{systemData.Name}Entity : {string.Join(", ", enumerable)}{{ }}";
                    stringBuilder.AppendLine(interfaceDefinition);
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

        context.AddSource(FileNameHint(systemData.Namespace, systemData.Name), stringBuilder.ToString());
    }

    static void GenerateComponentOutput(SourceProductionContext context, ComponentData data)
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

    static void GenerateEntityExtensionsOutput(SourceProductionContext context, (ComponentData ComponentData, ContextData ContextData) value)
    {
        var componentData = value.ComponentData;
        var contextData = value.ContextData;

        var entityExtensions = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentOutput));

        try
        {
            // entityExtensions.AppendLine("public partial class GuguGaga{}");
            var methodSignature = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
            // var methodSignatureWithLeadingComma = methodSignature == string.Empty ? string.Empty : $", {methodSignature}";
            var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));
            var equalityComparer = componentData.Fields.Length == 0 ? string.Empty : string.Join("", componentData.Fields.Select(field => $" && this.{componentData.Name}.{field.Name} == {field.ValidLowerName}"));


            var entityExtensionsContent =
                $$"""
                  partial class {{contextData.Prefix}}Entity : {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Entity
                  {
                      {{componentData.FullName}} {{componentData.Name}};

                      public bool Has{{componentData.Prefix}}() => this.{{componentData.Name}} != null;

                      public {{componentData.FullName}} Get{{componentData.Prefix}}() => this.{{componentData.Name}};

                      public {{contextData.Prefix}}Entity Set{{componentData.Prefix}}({{methodSignature}})
                      {
                          if (!this.IsEnabled)
                          {
                              return this;
                          }

                          if (this.{{componentData.Name}} == null)
                          {
                              this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});
                              return this;
                          }

                          if (Has{{componentData.Prefix}}(){{equalityComparer}})
                          {
                              return this;
                          }

                          var previousComponent = this.{{componentData.Name}};
                          this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});

                          {{componentData.FullName}}.DestroyComponent(previousComponent);
                          return this;
                      }

                      public {{contextData.Prefix}}Entity Remove{{componentData.Prefix}}()
                      {
                          if (!this.IsEnabled)
                          {
                              return this;
                          }

                          if (this.{{componentData.Name}} == null)
                          {
                              return this;
                          }

                          var previousComponent = this.{{componentData.Name}};
                          this.{{componentData.Name}} = null;

                          {{componentData.FullName}}.DestroyComponent(previousComponent);

                          return this;
                      }
                  }
                  """;

            using (new NamespaceBuilder(entityExtensions, contextData.Namespace))
            {
                entityExtensions.AppendLine(entityExtensionsContent);
            }
        }
        catch (Exception e)
        {
            entityExtensions.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint(contextData.Namespace, $"{contextData.Prefix}Entity{componentData.Prefix}Extensions"), entityExtensions.ToString());
    }

    static void GenerateContextExtensionsOutput(SourceProductionContext context, (ComponentData ComponentData, ContextData ContextData) value)
    {
        if (!value.ComponentData.IsUnique)
            return;

        var componentData = value.ComponentData;
        var contextData = value.ContextData;

        var contextExtensions = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentOutput));

        try
        {
            var methodSignature = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
            var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));
            var equalityComparer = componentData.Fields.Length == 0 ? string.Empty : string.Join("", componentData.Fields.Select(field => $" && this.{componentData.Name}.{field.Name} == {field.ValidLowerName}"));


            var contextExtensionsContent =
                $$"""
                  partial class {{contextData.Name}} : {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Context
                  {
                      {{componentData.FullName}} {{componentData.Name}};

                      public bool Has{{componentData.Prefix}}() => this.{{componentData.Name}} != null;

                      public {{componentData.FullName}} Get{{componentData.Prefix}}() => this.{{componentData.Name}};

                      public {{contextData.Name}} Set{{componentData.Prefix}}({{methodSignature}})
                      {
                          if (!this.IsEnabled)
                          {
                              return this;
                          }

                          if (this.{{componentData.Name}} == null)
                          {
                              this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});
                              return this;
                          }

                          if (Has{{componentData.Prefix}}(){{equalityComparer}})
                          {
                              return this;
                          }

                          var previousComponent = this.{{componentData.Name}};
                          this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});

                          {{componentData.FullName}}.DestroyComponent(previousComponent);
                          return this;
                      }

                      public {{contextData.Name}} Remove{{componentData.Prefix}}()
                      {
                          if (!this.IsEnabled)
                          {
                              return this;
                          }

                          if (this.{{componentData.Name}} == null)
                          {
                              return this;
                          }

                          var previousComponent = this.{{componentData.Name}};
                          this.{{componentData.Name}} = null;

                          {{componentData.FullName}}.DestroyComponent(previousComponent);

                          return this;
                      }
                  }
                  """;

            using (new NamespaceBuilder(contextExtensions, contextData.Namespace))
            {
                contextExtensions.AppendLine(contextExtensionsContent);
            }
        }
        catch (Exception e)
        {
            contextExtensions.AppendLine(e.ToString());
        }

        context.AddSource(FileNameHint(contextData.Namespace, $"{contextData.Name}{componentData.Prefix}Extensions"), contextExtensions.ToString());
    }

    static void GenerateInterfaceExtensionsOutput(SourceProductionContext context, ComponentWithContexts componentWithContexts)
    {
        var componentData = componentWithContexts.ComponentData;
        var contextDatas = componentWithContexts.ContextDatas;

        var methodSignature = string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
        var methodSignatureWithLeadingComma = methodSignature == string.Empty ? string.Empty : $", {methodSignature}";
        var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));

        var interfaceExtensions = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentOutput));

        var hasComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Has{componentData.Prefix}(),"));
        var getComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Get{componentData.Prefix}(),"));
        var setComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Set{componentData.Prefix}({methodArguments}),"));
        var removeComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullPrefix}Entity {x.Prefix}Entity => {x.Prefix}Entity.Remove{componentData.Prefix}(),"));
        var entityExtensions =
            $$"""
                public static class I{{componentData.Prefix}}Extensions
                {
                    public static bool Has{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity)
                    => entity switch
                    {
                    {{hasComponent}}
                        _ => default
                    };
                    public static {{componentData.FullName}} Get{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity)
                    => entity switch
                    {
                    {{getComponent}}
                        _ => default
                    };
                    public static I{{componentData.Prefix}}Entity Set{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity{{methodSignatureWithLeadingComma}})
                    => entity switch
                    {
                    {{setComponent}}
                        _ => default
                    };
                    public static I{{componentData.Prefix}}Entity Remove{{componentData.Prefix}}(this I{{componentData.Prefix}}Entity entity)
                    => entity switch
                    {
                    {{removeComponent}}
                        _ => default
                    };
              """;

        using (new NamespaceBuilder(interfaceExtensions, componentData.Namespace))
        {
            if (componentData.IsUnique)
                interfaceExtensions.AppendLine($"public interface I{componentData.Prefix}Context : Entitas.IContext {{ }} \n");

            interfaceExtensions.AppendLine($"public interface I{componentData.Prefix}Entity : Entitas.IEntity {{ }} \n");
            interfaceExtensions.AppendLine(entityExtensions);

             if (componentData.IsUnique)
             {
                 var contextHasComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Has{componentData.Prefix}(),"));
                 var contextGetComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Get{componentData.Prefix}(),"));
                 var contextSetComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Set{componentData.Prefix}({methodArguments}),"));
                 var contextRemoveComponent = contextDatas.Length == 0 ? string.Empty : string.Join("\n", contextDatas.Select(x => $"{x.FullName} {x.Name} => {x.Name}.Remove{componentData.Prefix}(),"));
                 var contextExtensions =
                     $$"""
                             public static bool Has{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context)
                             => context switch
                             {
                                {{contextHasComponent}}
                                 _ => default
                             };
                             public static {{componentData.FullName}} Get{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context)
                             => context switch
                             {
                                {{contextGetComponent}}
                                 _ => default
                             };
                             public static I{{componentData.Prefix}}Context Set{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context{{methodSignatureWithLeadingComma}})
                             => context switch
                             {
                                {{contextSetComponent}}
                                 _ => default
                             };
                             public static I{{componentData.Prefix}}Context Remove{{componentData.Prefix}}(this I{{componentData.Prefix}}Context context)
                             => context switch
                             {
                                {{contextRemoveComponent}}
                                 _ => default
                             };
                       """;
                 interfaceExtensions.AppendLine(contextExtensions);
             }

            interfaceExtensions.AppendLine("}");
        }

        context.AddSource(FileNameHint(componentData.Namespace, $"I{componentData.Prefix}EntityExtensions"), interfaceExtensions.ToString());
    }

    static void GenerateEntityOutput(SourceProductionContext context, ContextWithComponents value)
    {
        var contextData = value.ContextData;
        var componentDatas = value.ComponentDatas;

        var entityComponents = componentDatas.Where(x => !x.IsUnique).ToArray();
        // var interfaceImplementations = componentDatas.Length == 0 ? string.Empty : string.Join("", componentDatas.Select(static component => $", {component.Namespace.NamespaceClassifier()}I{component.Prefix}Entity"));
        // var fieldDeclarations = componentDatas.Length == 0 ? string.Empty : string.Join("\n", componentDatas.Select(static component => $"{component.FullName} {component.Name};"));
        var destroyCalls = entityComponents.Length == 0 ? string.Empty : string.Join("\n", entityComponents.Select(static component => $"{component.FullName}.DestroyComponent(this.{component.Name}); \n this.{component.Name} = null;"));

        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
                            public sealed partial class {{contextData.Prefix}}Entity : Entitas.EntityBase
                            {
                                public {{contextData.Name}} Context { get; private set; }
                                internal {{contextData.Prefix}}Entity(){}


                                public void InitializeEntity(int id, {{contextData.Name}} context)
                                {
                                    Id = id;
                                    Context = context;
                                    IsEnabled = true;
                                }
                                public void DestroyImmediate()
                                {
                            {{destroyCalls}}
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

    static void GenerateContextOutput(SourceProductionContext context, ContextWithComponents value)
    {
        var contextData = value.ContextData;
        var componentDatas = value.ComponentDatas;
        var strings = componentDatas.Length == 0 ? string.Empty : string.Join(", ", componentDatas.Select(static component => $"\"{component.Name}\""));
        var types = componentDatas.Length == 0 ? string.Empty : string.Join(", ", componentDatas.Select(static component => $"typeof({component.FullName})"));

        // var uniqueComponents = componentDatas.Where(x => x.IsUnique).ToArray();
        // var fieldDeclarations = uniqueComponents.Length == 0 ? string.Empty : string.Join("\n", uniqueComponents.Select(static component => $"{component.FullName} {component.Name};"));

        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
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

                                private {{contextData.Name}}()
                                {
                                    Id = _creationIndex++;
                                    IsEnabled = true;
                                    contextEntity = CreateEntity();
                                }

                                public static {{contextData.Name}} CreateContext()
                                {
                                    var context = new {{contextData.Name}}()
                                    {
                                    };

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
                                    _enabledEntities.Remove(entity.Id);
                                    EntityPool.Push(entity);
                                }
                            }
                            """;

            stringBuilder.AppendGenerationWarning(nameof(GenerateContextOutput));

            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(content);
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

        context.AddSource(FileNameHint(contextData.Namespace, contextData.Name), stringBuilder.ToString());
    }
}
