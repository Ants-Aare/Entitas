﻿//HintName: MyFeature.SomeNamespacedComponent.MyApp.Main.EntityExtension.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by
//     Entitas.Generators.ComponentGenerator.EntityExtension
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using global::MyApp.Main;
using static global::MyFeature.MyAppMainSomeNamespacedComponentIndex;

namespace MyFeature
{
public static class MyAppMainSomeNamespacedEntityExtension
{
    static readonly SomeNamespacedComponent SingleSomeNamespacedComponent = new SomeNamespacedComponent();

    public static bool HasSomeNamespaced(this Entity entity)
    {
        return entity.HasComponent(Index.Value);
    }

    public static Entity AddSomeNamespaced(this Entity entity)
    {
        entity.AddComponent(Index.Value, SingleSomeNamespacedComponent);
        return entity;
    }

    public static Entity ReplaceSomeNamespaced(this Entity entity)
    {
        entity.ReplaceComponent(Index.Value, SingleSomeNamespacedComponent);
        return entity;
    }

    public static Entity RemoveSomeNamespaced(this Entity entity)
    {
        entity.RemoveComponent(Index.Value);
        return entity;
    }

    public static SomeNamespacedComponent GetSomeNamespaced(this Entity entity)
    {
        return (SomeNamespacedComponent)entity.GetComponent(Index.Value);
    }
}
}
