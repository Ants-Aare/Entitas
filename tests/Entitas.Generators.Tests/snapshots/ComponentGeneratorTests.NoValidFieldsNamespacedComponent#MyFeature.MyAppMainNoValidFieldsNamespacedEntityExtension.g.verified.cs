﻿//HintName: MyFeature.MyAppMainNoValidFieldsNamespacedEntityExtension.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by
//     Entitas.Generators.ComponentGenerator.EntityExtension
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace MyFeature
{
public static class MyAppMainNoValidFieldsNamespacedEntityExtension
{
    static readonly NoValidFieldsNamespacedComponent SingleNoValidFieldsNamespacedComponent = new NoValidFieldsNamespacedComponent();

    public static bool HasNoValidFieldsNamespaced(this global::MyApp.Main.Entity entity)
    {
        return entity.HasComponent(MyAppMainNoValidFieldsNamespacedComponentIndex.Index.Value);
    }

    public static global::MyApp.Main.Entity AddNoValidFieldsNamespaced(this global::MyApp.Main.Entity entity)
    {
        entity.AddComponent(MyAppMainNoValidFieldsNamespacedComponentIndex.Index.Value, SingleNoValidFieldsNamespacedComponent);
        return entity;
    }

    public static global::MyApp.Main.Entity ReplaceNoValidFieldsNamespaced(this global::MyApp.Main.Entity entity)
    {
        entity.ReplaceComponent(MyAppMainNoValidFieldsNamespacedComponentIndex.Index.Value, SingleNoValidFieldsNamespacedComponent);
        return entity;
    }

    public static global::MyApp.Main.Entity RemoveNoValidFieldsNamespaced(this global::MyApp.Main.Entity entity)
    {
        entity.RemoveComponent(MyAppMainNoValidFieldsNamespacedComponentIndex.Index.Value);
        return entity;
    }

    public static NoValidFieldsNamespacedComponent GetNoValidFieldsNamespaced(this global::MyApp.Main.Entity entity)
    {
        return (NoValidFieldsNamespacedComponent)entity.GetComponent(MyAppMainNoValidFieldsNamespacedComponentIndex.Index.Value);
    }
}
}
