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
        var extendedSystemDatas = systemDatas
            .Combine(components)
            .Select(CombineSystemsWithComponents);
        var extendedFeatureDatas = featureDatas
            .Combine(components)
            .Select((x, _) => (x.Left, x.Right.Where(y => x.Left.Components.Contains(y.Name)).ToImmutableArray()));

        var extendedComponentDatasWithSystems = extendedComponentDatas
            .SelectMany((x, _) => x.ContextDatas.Select(contextData => (x.ComponentData, ContextData: contextData))).Combine(systems)
            .Select(CombineWithSystems);

        initContext.RegisterSourceOutput(componentDatas, GenerateComponent.GenerateComponentOutput);
        initContext.RegisterSourceOutput(extendedComponentDatas, GenerateInterfaceExtensions.GenerateInterfaceExtensionsOutput);

        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateEntityExtensions.GenerateEntityExtensionsOutput);
        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateContextExtensions.GenerateContextExtensionsOutput);

        initContext.RegisterSourceOutput(extendedSystemDatas, GenerateSystem.GenerateSystemOutput);

        initContext.RegisterSourceOutput(extendedContextDatas, GenerateEntity.GenerateEntityOutput);
        initContext.RegisterSourceOutput(extendedContextDatas, GenerateContext.GenerateContextOutput);

        initContext.RegisterSourceOutput(extendedFeatureDatas, GenerateFeature.GenerateFeatureOutput);
    }

    static ContextData AddFeature((ContextData contextData, ImmutableArray<FeatureData> features) data, CancellationToken ct)
    {
        var features = data.features
            .Where(featureData => data.contextData.Features.Contains(featureData.Name)
                                  || featureData.ManuallyAddedContexts.Contains(data.contextData.Name))
            .ToList();

        foreach (var feature in features)
        {
            data.contextData.Components = data.contextData.Components.AddRange(feature.Components);
            data.contextData.Systems = data.contextData.Systems.AddRange(feature.Systems);
        }
        data.contextData.Features = features.Select(x=> x.FullName).ToImmutableArray();

        return data.contextData;
    }

    static ExtendedComponentDataWithSystems CombineWithSystems(((ComponentData ComponentData, ContextData ContextData) data, ImmutableArray<SystemData> systemDatas) values, CancellationToken arg2)
    {
        var systems = values.systemDatas
            .Where(systemData => systemData.IsReactiveSystem
                                 && systemData.TriggeredBy.Any(x => x.component == values.data.ComponentData.Name))
            .Where(systemData => values.data.ContextData.Systems.Contains(systemData.Name)
                                 || systemData.ManuallyAddedContexts.Contains(values.data.ContextData.Name))
            .ToImmutableArray();

        return new ExtendedComponentDataWithSystems(values.data.ComponentData, values.data.ContextData, systems);
    }

    static ExtendedComponentData CombineComponentsWithContexts((ComponentData componentData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
    {
        var contexts = data.contextDatas
            .Where(contextData => data.componentData.ManuallyAddedContexts.Contains(contextData.Name)
                                  || contextData.Components.Contains(data.componentData.Name))
            .ToImmutableArray();

        return new ExtendedComponentData(data.componentData, contexts);
    }

    static ExtendedSystemData CombineSystemsWithComponents((SystemData systemData, ImmutableArray<ComponentData> componentDatas) data, CancellationToken ct)
    {
        var componentDatas = data.componentDatas
            .Where(componentData => data.systemData.TriggeredBy.Select(x => x.component).Contains(componentData.Name)
                                    || data.systemData.EntityIs.Contains(componentData.Name))
            .ToImmutableArray();

        return new ExtendedSystemData(data.systemData, componentDatas);
    }

    static ExtendedContextData CombineContextsWithComponents(((ContextData contextData, ImmutableArray<ComponentData> componentDatas) Left, ImmutableArray<SystemData> systemDatas) data, CancellationToken ct)
    {
        var contexts = data.Left.componentDatas
            .Where(componentData => data.Left.contextData.Components.Contains(componentData.Name)
                                    || componentData.ManuallyAddedContexts.Contains(data.Left.contextData.Name))
            .ToImmutableArray();

        var systems = data.systemDatas
            .Where(systemData => data.Left.contextData.Systems.Contains(systemData.Name)
                                 || systemData.ManuallyAddedContexts.Contains(data.Left.contextData.Name))
            .ToImmutableArray();

        return new ExtendedContextData(data.Left.contextData, contexts, systems);
    }
}
