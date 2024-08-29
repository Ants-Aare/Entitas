using System;
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

        var systems = systemDatas.Collect().Sort((x,y)=>
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

        var extendedComponentDatasWithSystems = extendedComponentDatas.SelectMany((x, _) => x.ContextDatas.Select(contextData => (x.ComponentData, ContextData: contextData))).Combine(systems)
            .Select(CombineWithSystems);

        initContext.RegisterSourceOutput(componentDatas, GenerateComponent.GenerateComponentOutput);
        initContext.RegisterSourceOutput(extendedComponentDatas, GenerateInterfaceExtensions.GenerateInterfaceExtensionsOutput);

        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateEntityExtensions.GenerateEntityExtensionsOutput);
        initContext.RegisterSourceOutput(extendedComponentDatasWithSystems, GenerateContextExtensions.GenerateContextExtensionsOutput);

        initContext.RegisterSourceOutput(extendedSystemDatas, GenerateSystem.GenerateSystemOutput);

        initContext.RegisterSourceOutput(extendedContextDatas, GenerateEntity.GenerateEntityOutput);
        initContext.RegisterSourceOutput(extendedContextDatas, GenerateContext.GenerateContextOutput);
    }

    ExtendedComponentDataWithSystems CombineWithSystems(((ComponentData ComponentData, ContextData ContextData) data, ImmutableArray<SystemData> systemDatas) values, CancellationToken arg2)
    {
        var systems = new List<SystemData>();
        foreach (var systemData in values.systemDatas)
        {
            if(!systemData.IsReactiveSystem)
                continue;

            if (systemData.TriggeredBy.Any(x => x.component == values.data.ComponentData.Name))
            {
                if(values.data.ContextData.Systems.Contains(systemData.Name))
                {
                    systems.Add(systemData);
                    continue;
                }

                if(systemData.ComponentAddedContexts.Contains(values.data.ContextData.Name))
                {
                    systems.Add(systemData);
                }
            }
        }

        return new ExtendedComponentDataWithSystems(values.data.ComponentData, values.data.ContextData, systems.ToImmutableArray());
    }

    static ExtendedComponentData CombineComponentsWithContexts((ComponentData componentData, ImmutableArray<ContextData> contextDatas) data, CancellationToken arg2)
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

        return new ExtendedComponentData(data.componentData, contexts.ToImmutableArray());
    }

    static ExtendedSystemData CombineSystemsWithComponents((SystemData systemData, ImmutableArray<ComponentData> componentDatas) data, CancellationToken ct)
    {
        var componentDatas = new List<ComponentData>();
        foreach (var componentData in data.componentDatas)
        {
            if (data.systemData.TriggeredBy.Select(x => x.component).Contains(componentData.Name))
            {
                componentDatas.Add(componentData);
                continue;
            }

            if (data.systemData.EntityIs.Contains(componentData.Name))
            {
                componentDatas.Add(componentData);
            }
        }

        return new ExtendedSystemData(data.systemData, componentDatas.ToImmutableArray());
    }

    static ExtendedContextData CombineContextsWithComponents(((ContextData contextData, ImmutableArray<ComponentData> componentDatas) Left, ImmutableArray<SystemData> systemDatas) data, CancellationToken ct)
    {
        var contexts = new List<ComponentData>();
        foreach (var componentData in data.Left.componentDatas)
        {
            if (data.Left.contextData.Components.Contains(componentData.Name))
            {
                contexts.Add(componentData);
                continue;
            }

            if (componentData.ComponentAddedContexts.Contains(data.Left.contextData.Name))
            {
                contexts.Add(componentData);
            }
        }

        var systems = new List<SystemData>();
        foreach (var systemData in data.systemDatas)
        {
            if (data.Left.contextData.Systems.Contains(systemData.Name))
            {
                systems.Add(systemData);
                continue;
            }

            if (systemData.ComponentAddedContexts.Contains(data.Left.contextData.Name))
            {
                systems.Add(systemData);
            }
        }

        return new ExtendedContextData(data.Left.contextData, contexts.ToImmutableArray(), systems.ToImmutableArray());
    }
}
