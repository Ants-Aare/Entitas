﻿//HintName: MyApp.Main.MultipleFieldsEntityExtension.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by
//     Entitas.Generators.ComponentGenerator.EntityExtension
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace MyApp.Main
{
public static class MultipleFieldsEntityExtension
{
    public static bool HasMultipleFields(this Entity entity)
    {
        return entity.HasComponent(MultipleFieldsComponentIndex.Value);
    }

    public static Entity AddMultipleFields(this Entity entity, string value1, string value2, string value3)
    {
        var index = MultipleFieldsComponentIndex.Value;
        var component = (MultipleFieldsComponent)entity.CreateComponent(index, typeof(MultipleFieldsComponent));
        component.Value1 = value1;
        component.Value2 = value2;
        component.Value3 = value3;
        entity.AddComponent(index, component);
        return entity;
    }

    public static Entity ReplaceMultipleFields(this Entity entity, string value1, string value2, string value3)
    {
        var index = MultipleFieldsComponentIndex.Value;
        var component = (MultipleFieldsComponent)entity.CreateComponent(index, typeof(MultipleFieldsComponent));
        component.Value1 = value1;
        component.Value2 = value2;
        component.Value3 = value3;
        entity.ReplaceComponent(index, component);
        return entity;
    }

    public static Entity RemoveMultipleFields(this Entity entity)
    {
        entity.RemoveComponent(MultipleFieldsComponentIndex.Value);
        return entity;
    }
}
}
