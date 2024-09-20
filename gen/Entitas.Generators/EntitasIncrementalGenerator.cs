using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Entitas.Generators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Generators;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class EntitasIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var listenerDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ListenerData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ListenerData>)
            .RemoveEmptyValues();
        var listenerEvents = listenerDatas.SelectMany((x, _) => x.Events)
            .RemoveDuplicates();

        var archetypeDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ArchetypeData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ArchetypeData>)
            .RemoveEmptyValues();

        var groupDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(GroupData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<GroupData>)
            .RemoveEmptyValues()
            .RemoveDuplicates(GroupData.ComponentComparer);

        var featureDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(FeatureData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<FeatureData>)
            .RemoveEmptyValues();

        var systemDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(SystemData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<SystemData>)
            .RemoveEmptyValues()
            .RemoveDuplicates();

        var componentDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ComponentData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ComponentData>)
            .RemoveEmptyValues()
            .RemoveDuplicates();

        var contextDatas = initContext.SyntaxProvider
            .CreateSyntaxProvider(ContextData.SyntaxFilter, SyntaxTransformer.TransformClassDeclarationTo<ContextData>)
            .RemoveEmptyValues();

        //Add events

        componentDatas.Combine(listenerEvents.WithComparer(EventData.TypeAndEventComparer).Collect())
            .Select(AddEventsToComponent)

        //Add components and systems from features
        var features = featureDatas.Collect();
        contextDatas = contextDatas.Combine(features).Select(AddFeatureToContext);
        archetypeDatas = archetypeDatas.Combine(features).Select(AddFeatureToArchetype);


        var systems = systemDatas.Collect().Sort();
        var components = componentDatas.Collect().Sort();
        var contexts = contextDatas.Collect().Sort();
        var groups = groupDatas.Collect().Sort();

        var extendedArchetypeDatas = archetypeDatas
            .Combine(components)
            .Combine(contexts)
            .Select(CombineArchetypesWithContexts);

        var extendedComponentDatas = componentDatas
            .Combine(contexts)
            .Select(CombineComponentsWithContexts);

        var extendedContextDatas = contextDatas
            .Combine(components)
            .Combine(systems)
            .Combine(groups)
            .Select(CombineContextsWithComponents);

        var extendedGroupDatas = groupDatas
            .Combine(extendedContextDatas.Select((x, _) => x.ContextData).Collect())
            .Select(CombineGroupsWithContexts);

        var extendedComponentDatasWithSystems = extendedComponentDatas
            .SelectMany((x, _) => x.ContextDatas.Select(contextData => (x.ComponentData, contextData)))
            .Combine(systems)
            .Combine(groups)
            .Select(CombineComponentsWithSystemsAndGroups);


        var cleanupSystems = componentDatas
            .Where(x => x.IsCleanup)
            .Select((x, _) => SystemData.CreateCleanupSystem(x))
            .Collect()
            .Sort(SystemData.CleanupOrderComparer);
        var allSystems = systems.Append(cleanupSystems);

        initContext.RegisterSourceOutput(componentDatas, GenerateComponent.GenerateComponentOutput);
        initContext.RegisterSourceOutput(extendedComponentDatas, GenerateInterfaceExtensions.GenerateInterfaceExtensionsOutput);

        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateEntityExtensions.GenerateEntityExtensionsOutput);
        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateContextExtensions.GenerateContextExtensionsOutput);

        initContext.RegisterSourceOutput(systemDatas, GenerateSystem.GenerateSystemOutput);
        initContext.RegisterSourceOutput(allSystems, GenerateSystemUpdateLoop.GenerateSystemUpdateLoopOutput);

        initContext.RegisterSourceOutput(extendedContextDatas, GenerateEntity.GenerateEntityOutput);
        initContext.RegisterSourceOutput(extendedContextDatas, GenerateContext.GenerateContextOutput);

        initContext.RegisterSourceOutput(listenerDatas, GenerateListener.GenerateListenerOutput);
        initContext.RegisterSourceOutput(featureDatas, GenerateFeature.GenerateFeatureOutput);
        initContext.RegisterSourceOutput(groupDatas, GenerateGroup.GenerateGroupOutput);
        initContext.RegisterSourceOutput(extendedGroupDatas, GenerateGroup.GenerateGroupExtensionsOutput);
        initContext.RegisterSourceOutput(extendedArchetypeDatas, GenerateArchetype.GenerateArchetypeOutputs);
    }

    ComponentData AddEventsToComponent((ComponentData componentData, ImmutableArray<EventData> eventDatas) data, CancellationToken arg2)
    {
        var componentData = data.componentData;
        var events = new List<EventData>();
        foreach (var eventData in data.eventDatas.Where(x => x.Type == componentData.TypeData))
        {
            if(events.Contains())
            events.Add();
        }
        componentData.Events = .ToImmutableArray();
        return componentData;
    }

    ExtendedArchetypeData CombineArchetypesWithContexts(((ArchetypeData archetypeData, ImmutableArray<ComponentData> componentDatas) Left, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
    {
        var archetypeData = data.Left.archetypeData;
        var components = data.Left.componentDatas
            .Where(component => archetypeData.Components.Any(x => x.TypeData == component.TypeData))
            .ToImmutableArray();

        var contexts = data.contextDatas
            .Where(context => archetypeData.Components
                .All(x => context.Components.Any(y => y == x.TypeData)
                          || components.Any(y => y.Contexts.Any(z => z == context.TypeData))))
            .ToImmutableArray();

        return new ExtendedArchetypeData(archetypeData, contexts, components);
    }

    static ContextData AddFeatureToContext((ContextData contextData, ImmutableArray<FeatureData> features) data, CancellationToken ct)
    {
        var features = data.features
            .Where(featureData => data.contextData.Features.Contains(featureData.TypeData)
                                  || featureData.Contexts.Contains(data.contextData.TypeData))
            .ToList();

        foreach (var feature in features)
        {
            data.contextData.Components = data.contextData.Components.AddRange(feature.Components);
            data.contextData.Systems = data.contextData.Systems.AddRange(feature.Systems);
        }

        data.contextData.Features = features.Select(x => x.TypeData).ToImmutableArray();

        return data.contextData;
    }

    static ArchetypeData AddFeatureToArchetype((ArchetypeData archetypeData, ImmutableArray<FeatureData> features) data, CancellationToken ct)
    {
        var components = data.features
            .Where(featureData => data.archetypeData.Features.Contains(featureData.TypeData))
            .SelectMany(feature => feature.Components
                .Where(x => !data.archetypeData.Components.Any(y => y.TypeData == x))
                .Select(x => new ArchetypeComponentData(x, false, ImmutableArray<string>.Empty))
            );

        data.archetypeData.Components = data.archetypeData.Components.AddRange(components).Sort();
        return data.archetypeData;
    }

    static ExtendedComponentDataWithSystemsAndGroups CombineComponentsWithSystemsAndGroups((((ComponentData ComponentData, ContextData ContextData) data, ImmutableArray<SystemData> systemDatas) values, ImmutableArray<GroupData> groupDatas) arg1, CancellationToken arg2)
    {
        var systemDatas = arg1.values.systemDatas;
        var groupDatas = arg1.groupDatas;
        var componentData = arg1.values.data.ComponentData;
        var contextData = arg1.values.data.ContextData;

        var systems = systemDatas
            .Where(systemData => systemData.IsReactiveSystem && systemData.TriggeredBy.Any(x => x.component == componentData.TypeData))
            .Where(systemData => contextData.Systems.Contains(systemData.TypeData) || systemData.Contexts.Contains(contextData.TypeData))
            .ToImmutableArray();

        var groups = componentData.IsUnique
            ? ImmutableArray<GroupData>.Empty
            : groupDatas
                .Where(x => x.GetAllTypes.Contains(componentData.TypeData))
                .ToImmutableArray();

        return new ExtendedComponentDataWithSystemsAndGroups(componentData, contextData, systems, groups);
    }

    static ExtendedGroupData CombineGroupsWithContexts((GroupData groupData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
    {
        var contexts = data.contextDatas
            .Where(contextData => data.groupData.ContainsAllNecessaryComponents(contextData))
            .ToImmutableArray();

        return new ExtendedGroupData(data.groupData, contexts);
    }

    static ExtendedComponentData CombineComponentsWithContexts((ComponentData componentData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
    {
        var contexts = data.contextDatas
            .Where(contextData => data.componentData.Contexts.Contains(contextData.TypeData)
                                  || contextData.Components.Contains(data.componentData.TypeData))
            .ToImmutableArray();

        data.componentData.Contexts = contexts.Select(x => x.TypeData).ToImmutableArray();
        return new ExtendedComponentData(data.componentData, contexts);
    }

    ExtendedContextData CombineContextsWithComponents((((ContextData contextData, ImmutableArray<ComponentData> componentDatas) Left, ImmutableArray<SystemData> systemDatas) data, ImmutableArray<GroupData> groupDatas) arg1, CancellationToken arg2)
    {
        var contextData = arg1.data.Left.contextData;
        var componentDatas = arg1.data.Left.componentDatas;
        var systemDatas = arg1.data.systemDatas;
        var groupDatas = arg1.groupDatas;

        var components = componentDatas
            .Where(componentData => contextData.Components.Contains(componentData.TypeData)
                                    || componentData.Contexts.Contains(contextData.TypeData))
            .ToImmutableArray();

        var systems = systemDatas
            .Where(systemData => contextData.Systems.Contains(systemData.TypeData)
                                 || systemData.Contexts.Contains(contextData.TypeData))
            .ToImmutableArray();

        var groups = groupDatas
            .Where(x => x.ContainsAllNecessaryComponents(components))
            .ToImmutableArray();

        contextData.Components = components.Select(x => x.TypeData).ToImmutableArray();
        return new ExtendedContextData(contextData, components, systems, groups);
    }
}
