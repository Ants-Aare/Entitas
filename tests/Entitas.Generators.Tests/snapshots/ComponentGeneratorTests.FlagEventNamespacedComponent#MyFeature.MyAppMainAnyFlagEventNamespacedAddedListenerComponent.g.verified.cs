﻿//HintName: MyFeature.MyAppMainAnyFlagEventNamespacedAddedListenerComponent.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by
//     Entitas.Generators.ComponentGenerator.Events
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace MyFeature
{
public interface IMyAppMainAnyFlagEventNamespacedAddedListener
{
    void OnAnyFlagEventNamespacedAdded(global::MyApp.Main.Entity entity);
}

public sealed class MyAppMainAnyFlagEventNamespacedAddedListenerComponent : global::Entitas.IComponent
{
    public global::System.Collections.Generic.List<IMyAppMainAnyFlagEventNamespacedAddedListener> Value;
}

public static class MyAppMainAnyFlagEventNamespacedAddedListenerEventEntityExtension
{
    public static global::MyApp.Main.Entity AddAnyFlagEventNamespacedAddedListener(this global::MyApp.Main.Entity entity, IMyAppMainAnyFlagEventNamespacedAddedListener value)
    {
        var listeners = entity.HasAnyFlagEventNamespacedAddedListener()
            ? entity.GetAnyFlagEventNamespacedAddedListener().Value
            : new global::System.Collections.Generic.List<IMyAppMainAnyFlagEventNamespacedAddedListener>();
        listeners.Add(value);
        return entity.ReplaceAnyFlagEventNamespacedAddedListener(listeners);
    }

    public static void RemoveAnyFlagEventNamespacedAddedListener(this global::MyApp.Main.Entity entity, IMyAppMainAnyFlagEventNamespacedAddedListener value, bool removeListenerWhenEmpty = true)
    {
        var listeners = entity.GetAnyFlagEventNamespacedAddedListener().Value;
        listeners.Remove(value);
        if (removeListenerWhenEmpty && listeners.Count == 0)
        {
            entity.RemoveAnyFlagEventNamespacedAddedListener();
            if (entity.IsEmpty())
                entity.Destroy();
        }
        else
        {
            entity.ReplaceAnyFlagEventNamespacedAddedListener(listeners);
        }
    }
}

public sealed class MyAppMainAnyFlagEventNamespacedAddedEventSystem : global::Entitas.ReactiveSystem<global::MyApp.Main.Entity>
{
    readonly global::Entitas.IGroup<global::MyApp.Main.Entity> _listeners;
    readonly global::System.Collections.Generic.List<global::MyApp.Main.Entity> _entityBuffer;
    readonly global::System.Collections.Generic.List<IMyAppMainAnyFlagEventNamespacedAddedListener> _listenerBuffer;

    public MyAppMainAnyFlagEventNamespacedAddedEventSystem(MyApp.MainContext context) : base(context)
    {
        _listeners = context.GetGroup(MyAppMainAnyFlagEventNamespacedAddedListenerMatcher.AnyFlagEventNamespacedAddedListener);
        _entityBuffer = new global::System.Collections.Generic.List<global::MyApp.Main.Entity>();
        _listenerBuffer = new global::System.Collections.Generic.List<IMyAppMainAnyFlagEventNamespacedAddedListener>();
    }

    protected override global::Entitas.ICollector<global::MyApp.Main.Entity> GetTrigger(global::Entitas.IContext<global::MyApp.Main.Entity> context)
    {
        return global::Entitas.CollectorContextExtension.CreateCollector(
            context, global::Entitas.TriggerOnEventMatcherExtension.Added(MyAppMainFlagEventNamespacedMatcher.FlagEventNamespaced)
        );
    }

    protected override bool Filter(global::MyApp.Main.Entity entity)
    {
        return entity.HasFlagEventNamespaced();
    }

    protected override void Execute(global::System.Collections.Generic.List<global::MyApp.Main.Entity> entities)
    {
        foreach (var entity in entities)
        {
            foreach (var listenerEntity in _listeners.GetEntities(_entityBuffer))
            {
                _listenerBuffer.Clear();
                _listenerBuffer.AddRange(listenerEntity.GetAnyFlagEventNamespacedAddedListener().Value);
                foreach (var listener in _listenerBuffer)
                {
                    listener.OnAnyFlagEventNamespacedAdded(entity);
                }
            }
        }
    }
}
}
