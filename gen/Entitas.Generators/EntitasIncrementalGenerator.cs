using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Entitas.Generators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

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

        initContext.RegisterSourceOutput(componentDatas, GenerateComponent.GenerateComponentOutput);
        initContext.RegisterSourceOutput(componentContextPair, GenerateEntityExtensions.GenerateEntityExtensionsOutput);
        initContext.RegisterSourceOutput(componentContextPair, GenerateContextExtensions.GenerateContextExtensionsOutput);
        initContext.RegisterSourceOutput(systemsWithComponents, GenerateSystem.GenerateSystemOutput);

        initContext.RegisterSourceOutput(contextsWithComponents, GenerateContext.GenerateContextOutput);
        initContext.RegisterSourceOutput(contextsWithComponents, GenerateEntity.GenerateEntityOutput);
        initContext.RegisterSourceOutput(componentsWithContexts, GenerateInterfaceExtensions.GenerateInterfaceExtensionsOutput);
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
}
