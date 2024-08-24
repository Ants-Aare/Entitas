using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;
using static Entitas.Generators.Templates;

namespace Entitas.Generators;

[Generator(LanguageNames.CSharp)]
public sealed partial class EntitasIncrementalGenerator : IIncrementalGenerator
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

        // var contextDatas = initContext.SyntaxProvider
        //     .CreateSyntaxProvider(ContextData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ContextData>)
        //     .RemoveEmptyValues();

        initContext.RegisterSourceOutput(componentDatas, GenerateComponentOutput);
        // initContext.RegisterSourceOutput(groupDatas, GenerateGroupOutput);
        // initContext.RegisterSourceOutput(systemDatas, GenerateSystemOutput);
        // initContext.RegisterSourceOutput(contextDatas, GenerateContextOutput);
    }

    void GenerateGroupOutput(SourceProductionContext context, GroupData data) { }

    void GenerateSystemOutput(SourceProductionContext context, SystemData data) { }

    void GenerateComponentOutput(SourceProductionContext context, ComponentData data)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendGenerationWarning(nameof(EntitasIncrementalGenerator));

        var content = $$"""
                        public sealed partial class {{data.Name}}
                        {
                        }
                        """;


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

    void GenerateContextOutput(SourceProductionContext context, ContextData data) { }
}
