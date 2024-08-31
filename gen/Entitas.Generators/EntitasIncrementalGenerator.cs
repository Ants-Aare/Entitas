using System;
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

        var featureDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(FeatureData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<FeatureData>)
            .RemoveEmptyValues();

        var systemDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(SystemData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<SystemData>)
            .RemoveEmptyValues();

        var componentDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ComponentData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ComponentData>)
            .RemoveEmptyValues();

        var contextDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ContextData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ContextData>)
            .RemoveEmptyValues();

        //Add components and systems from features
        contextDatas = contextDatas.Combine(featureDatas.Collect()).Select(AddFeature);

        var systems = systemDatas.Collect().Sort((x, y) =>
        {
            var reactiveOrder = x.ReactiveOrder.CompareTo(y.ReactiveOrder);
            return reactiveOrder == 0 ? string.Compare(x.Name, y.Name, StringComparison.Ordinal) : reactiveOrder;
        });
        var components = componentDatas.Collect();
        var contexts = contextDatas.Collect();


        var extendedComponentDatas = componentDatas
            .Combine(contexts)
            .Select(CombineComponentsWithContexts);
        var extendedContextDatas = contextDatas
            .Combine(components)
            .Combine(systems)
            .Select(CombineContextsWithComponents);

        var extendedComponentDatasWithSystems = extendedComponentDatas
            .SelectMany((x, _) => x.ContextDatas.Select(contextData => (x.ComponentData, contextData)))
            .Combine(systems)
            .Select(CombineWithSystems);

        initContext.RegisterSourceOutput(componentDatas, GenerateComponent.GenerateComponentOutput);
        initContext.RegisterSourceOutput(extendedComponentDatas, GenerateInterfaceExtensions.GenerateInterfaceExtensionsOutput);

        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateEntityExtensions.GenerateEntityExtensionsOutput);
        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateContextExtensions.GenerateContextExtensionsOutput);

        initContext.RegisterSourceOutput(systemDatas, GenerateSystem.GenerateSystemOutput);

        initContext.RegisterSourceOutput(extendedContextDatas, GenerateEntity.GenerateEntityOutput);
        initContext.RegisterSourceOutput(extendedContextDatas, GenerateContext.GenerateContextOutput);

        initContext.RegisterSourceOutput(featureDatas, GenerateFeature.GenerateFeatureOutput);
    }

    static ContextData AddFeature((ContextData contextData, ImmutableArray<FeatureData> features) data, CancellationToken ct)
    {
        var features = data.features
            .Where(featureData => data.contextData.Features.Contains(featureData.TypeData)
                                  || featureData.ManuallyAddedContexts.Contains(data.contextData.TypeData))
            .ToList();

        foreach (var feature in features)
        {
            data.contextData.Components = data.contextData.Components.AddRange(feature.Components);
            data.contextData.Systems = data.contextData.Systems.AddRange(feature.Systems);
        }
        data.contextData.Features = features.Select(x=> x.TypeData).ToImmutableArray();

        return data.contextData;
    }

    static ExtendedComponentDataWithSystems CombineWithSystems(((ComponentData ComponentData, ContextData ContextData) data, ImmutableArray<SystemData> systemDatas) values, CancellationToken arg2)
    {
        var systems = values.systemDatas
            .Where(systemData => systemData.IsReactiveSystem && systemData.TriggeredBy.Any(x => x.component == values.data.ComponentData.TypeData))
            .Where(systemData => values.data.ContextData.Systems.Contains(systemData.TypeData) || systemData.ManuallyAddedContexts.Contains(values.data.ContextData.TypeData))
            .ToImmutableArray();

        return new ExtendedComponentDataWithSystems(values.data.ComponentData, values.data.ContextData, systems);
    }

    static ExtendedComponentData CombineComponentsWithContexts((ComponentData componentData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
    {
        var contexts = data.contextDatas
            .Where(contextData => data.componentData.ManuallyAddedContexts.Contains(contextData.TypeData)
                                  || contextData.Components.Contains(data.componentData.TypeData))
            .ToImmutableArray();

        return new ExtendedComponentData(data.componentData, contexts);
    }

    static ExtendedContextData CombineContextsWithComponents(((ContextData contextData, ImmutableArray<ComponentData> componentDatas) Left, ImmutableArray<SystemData> systemDatas) data, CancellationToken ct)
    {
        var componentDatas = data.Left.componentDatas
            .Where(componentData => data.Left.contextData.Components.Contains(componentData.TypeData)
                                    || componentData.ManuallyAddedContexts.Contains(data.Left.contextData.TypeData))
            .ToImmutableArray();

        var systems = data.systemDatas
            .Where(systemData => data.Left.contextData.Systems.Contains(systemData.TypeData)
                                 || systemData.ManuallyAddedContexts.Contains(data.Left.contextData.TypeData))
            .ToImmutableArray();

        return new ExtendedContextData(data.Left.contextData, componentDatas, systems);
    }
}
