using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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

        // var allComponents = componentDatas
        //     .Select((x, _) => x.Prefix)
        //     .Collect()
        //     .Sort();

        // var contextDatas = initContext.SyntaxProvider
        //     .CreateSyntaxProvider(ContextData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ContextData>)
        //     .RemoveEmptyValues();

        // initContext.RegisterSourceOutput(allComponents, GenerateComponentsEnumOutput);
        // initContext.RegisterSourceOutput(componentDatas, GenerateInterfaceExtensionsOutput);
        initContext.RegisterSourceOutput(componentDatas, GenerateComponentOutput);
        // initContext.RegisterSourceOutput(groupDatas, GenerateGroupOutput);
        // initContext.RegisterSourceOutput(systemDatas, GenerateSystemOutput);
        // initContext.RegisterSourceOutput(contextDatas, GenerateContextOutput);
    }

    void GenerateComponentsEnumOutput(SourceProductionContext context, ImmutableArray<string> componentNames)
    {
        var enumsStringBuilder = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateComponentsEnumOutput));

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

        // var interfacesStringBuilder = new StringBuilder()
        //     .AppendGenerationWarning(nameof(GenerateComponentsEnumOutput));
        // using (new NamespaceBuilder(enumsStringBuilder, "Entitas.Generated"))
        // {
        //     foreach (var componentName in componentNames)
        //     {
        //         interfacesStringBuilder.AppendLine();
        //     }
        // }

        context.AddSource(FileNameHint("Entitas.Generated", "ComponentEnums"), enumsStringBuilder.ToString());
        // context.AddSource(FileNameHint("Entitas.Generated", "Interfaces"), interfacesStringBuilder.ToString());
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
            var ctorAssignments = data.Fields.Length == 0 ? string.Empty : string.Join("\n", data.Fields.Select(static field => $"{field.Name} = {field.ValidLowerName};"));
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

    void GenerateInterfaceExtensionsOutput(SourceProductionContext context, ComponentData data)
    {
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
            interfaceExtensions.AppendLine($"    public interface I{data.Prefix}Entity : Entitas.IEntity {{ }}");
            interfaceExtensions.AppendLine(interfaceExtensionsContent);
        }

        context.AddSource(FileNameHint(data.Namespace, $"I{data.Prefix}EntityExtensions"), interfaceExtensions.ToString());
    }

    void GenerateContextOutput(SourceProductionContext context, ContextData data)
    {
        var content = $$"""
                        public sealed partial class {{data.Name}}
                        {

                        }
                        """;

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendGenerationWarning(nameof(GenerateContextOutput));

        using (new NamespaceBuilder(stringBuilder, data.Namespace))
        {
            stringBuilder.AppendLine(content);
        }

        using (new CommentBuilder(stringBuilder))
        {
            stringBuilder.AppendLine(data.ToString());
        }

        context.AddSource(FileNameHint(data.Namespace, data.Name), stringBuilder.ToString());
    }
}
