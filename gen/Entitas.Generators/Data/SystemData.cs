using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Entitas.Generators.Common;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.Utility.StringConstants;

namespace Entitas.Generators.Data;

public struct SystemData() : IClassDeclarationResolver, IAttributeResolver, IConstructorResolver, IComparable<SystemData>, IEquatable<SystemData>
{
    public TypeData TypeData { get; private set; } = default;
    public string ValidLowerName { get; private set; } = null!;

    public bool IsInitializeSystem { get; private set; } = false;
    public int InitializeOrder { get; private set; } = 0;

    public bool IsTeardownSystem { get; private set; } = false;
    public int TeardownOrder { get; private set; } = 0;

    public bool IsReactiveSystem { get; private set; } = false;
    public SystemExecution ReactiveExecution { get; private set; } = SystemExecution.Manual;
    public int ReactiveOrder { get; private set; } = 0;
    public ImmutableArray<(TypeData component, ComponentEvent eventType)> TriggeredBy { get; private set; } = ImmutableArray<(TypeData, ComponentEvent)>.Empty;
    public ImmutableArray<TypeData> EntityIs { get; private set; } = ImmutableArray<TypeData>.Empty;

    public bool IsExecuteSystem { get; private set; } = false;
    public SystemExecution ExecuteExecution { get; private set; } = SystemExecution.Manual;
    public int ExecuteOrder { get; private set; } = 0;

    public bool IsCleanupSystem { get; private set; } = false;
    public SystemExecution CleanupExecution { get; private set; } = SystemExecution.Manual;
    public int CleanupOrder { get; private set; } = 0;

    public ImmutableArray<TypeData> Contexts { get; private set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<FieldData> ConstructorArguments { get; private set; } = ImmutableArray<FieldData>.Empty;

    public string? Namespace => TypeData.Namespace;
    public string FullName => TypeData.FullName;
    public string Name => TypeData.Name;

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken _)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: InitializeSystemName or InitializeSystemAttributeName or ReactiveSystemName or ReactiveSystemAttributeName or ExecuteSystemName or ExecuteSystemAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: InitializeSystemName or InitializeSystemAttributeName or ReactiveSystemName or ReactiveSystemAttributeName or ExecuteSystemName or ExecuteSystemAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        TypeData = TypeData.Create(symbol);
        ValidLowerName = Name.ToValidLowerName();

        return true;
    }

    public bool TryResolveConstructor(IMethodSymbol constructorMethod)
    {
        if (constructorMethod.Parameters.IsDefaultOrEmpty)
            return true;
        ConstructorArguments = constructorMethod.Parameters.Select(x => new FieldData(x.Type.ToDisplayString(), x.Name)).ToImmutableArray();
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: ReactiveSystemAttributeName } => TryResolveReactiveSystemAttribute(attributeData),
            { AttributeClass.Name: InitializeSystemAttributeName } => TryResolveInitializeSystemAttribute(attributeData),
            { AttributeClass.Name: TeardownSystemAttributeName } => TryResolveTeardownSystemAttribute(attributeData),
            { AttributeClass.Name: ExecuteSystemAttributeName } => TryResolveExecuteSystemAttribute(attributeData),
            { AttributeClass.Name: EntityIsAttributeName } => TryResolveEntityIsAttribute(attributeData),
            { AttributeClass.Name: AddToContextAttributeName } => TryResolveAddToContextAttribute(attributeData),
            { AttributeClass.Name: TriggeredByAttributeName } => TryResolveTriggeredByAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveTriggeredByAttribute(AttributeData attributeData)
    {
        var componentType = TypeData.Create((INamedTypeSymbol)attributeData.ConstructorArguments[0].Value!, ComponentName);
        var eventType = (ComponentEvent)(attributeData.ConstructorArguments[1].Value ?? ComponentEvent.Added);
        TriggeredBy = TriggeredBy.Add((componentType, eventType));
        return true;
    }

    bool TryResolveReactiveSystemAttribute(AttributeData attributeData)
    {
        IsReactiveSystem = true;
        ReactiveExecution = (SystemExecution)(attributeData.ConstructorArguments[0].Value ?? SystemExecution.Manual);
        ReactiveOrder = (int)(attributeData.ConstructorArguments[1].Value ?? 0);
        return true;
    }

    bool TryResolveInitializeSystemAttribute(AttributeData attributeData)
    {
        IsInitializeSystem = true;
        InitializeOrder = (int)(attributeData.ConstructorArguments[0].Value ?? 0);
        return true;
    }

    bool TryResolveTeardownSystemAttribute(AttributeData attributeData)
    {
        IsTeardownSystem = true;
        TeardownOrder = (int)(attributeData.ConstructorArguments[0].Value ?? 0);
        return true;
    }

    bool TryResolveExecuteSystemAttribute(AttributeData attributeData)
    {
        IsExecuteSystem = true;
        ExecuteExecution = (SystemExecution)(attributeData.ConstructorArguments[0].Value ?? SystemExecution.Manual);
        ExecuteOrder = (int)(attributeData.ConstructorArguments[1].Value ?? 0);
        return true;
    }

    bool TryResolveEntityIsAttribute(AttributeData attributeData)
    {
        EntityIs = attributeData.ConstructorArguments[0].Values
            .Where(x => x.Value is INamedTypeSymbol)
            .Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName))
            .ToImmutableArray();
        return true;
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var typedConstants = attributeData.ConstructorArguments[0].Values;
        Contexts = typedConstants.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ContextName)).ToImmutableArray();
        return true;
    }

    public static SystemData CreateCleanupSystem(ComponentData componentData)
    {
        var system = new SystemData
        {
            TypeData = componentData.TypeData,
            IsCleanupSystem = true,
            CleanupExecution = (SystemExecution)componentData.CleanupExecution,
            CleanupOrder = componentData.CleanupOrder,
        };
        return system;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("SystemData:\n");
        try
        {
            stringBuilder.AppendLine($"   {nameof(Namespace)}: {Namespace}")
                .AppendLine($"   {nameof(FullName)}: {FullName}")
                .AppendLine($"   {nameof(Name)}: {Name}")
                .AppendLine($"   {nameof(IsInitializeSystem)}: {IsInitializeSystem} {CleanupOrder}")
                .AppendLine($"   {nameof(IsTeardownSystem)}: {IsTeardownSystem} {TeardownOrder}")
                .AppendLine($"   {nameof(IsReactiveSystem)}: {IsReactiveSystem} {ReactiveExecution} {ReactiveOrder}")
                .AppendLine($"   {nameof(IsExecuteSystem)}: {IsExecuteSystem} {ExecuteExecution} {ExecuteOrder}")
                .AppendLine($"   {nameof(IsCleanupSystem)}: {IsCleanupSystem} {CleanupOrder}");

            if (TriggeredBy.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(TriggeredBy)}:");
                foreach (var data in TriggeredBy)
                {
                    stringBuilder.AppendLine($"      {data.component} {data.eventType}");
                }
            }

            if (EntityIs.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(EntityIs)}:");
                foreach (var components in EntityIs)
                {
                    stringBuilder.AppendLine($"      {components}");
                }
            }

            if (Contexts.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(Contexts)}:");
                foreach (var contexts in Contexts)
                {
                    stringBuilder.AppendLine($"      {contexts}");
                }
            }

            if (ConstructorArguments.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(ConstructorArguments)}:");
                foreach (var contexts in ConstructorArguments)
                {
                    stringBuilder.AppendLine($"      {contexts}");
                }
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        return stringBuilder.ToString();
    }

    public bool Equals(SystemData other)
    {
        return TypeData.Equals(other.TypeData)
               && IsInitializeSystem == other.IsInitializeSystem
               && InitializeOrder == other.InitializeOrder
               && IsTeardownSystem == other.IsTeardownSystem
               && TeardownOrder == other.TeardownOrder
               && IsReactiveSystem == other.IsReactiveSystem
               && ReactiveExecution == other.ReactiveExecution
               && ReactiveOrder == other.ReactiveOrder
               && IsExecuteSystem == other.IsExecuteSystem
               && ExecuteExecution == other.ExecuteExecution
               && ExecuteOrder == other.ExecuteOrder
               && IsCleanupSystem == other.IsCleanupSystem
               && CleanupExecution == other.CleanupExecution
               && CleanupOrder == other.CleanupOrder
               && TriggeredBy.SequenceEqual(other.TriggeredBy)
               && EntityIs.SequenceEqual(other.EntityIs)
               && Contexts.SequenceEqual(other.Contexts)
               && ConstructorArguments.Equals(other.ConstructorArguments);
    }

    public override bool Equals(object? obj)
    {
        return obj is SystemData other && Equals(other);
    }

    public override int GetHashCode() => TypeData.GetHashCode();

    public int CompareTo(SystemData other)
    {
        var reactiveOrder = ReactiveOrder.CompareTo(other.ReactiveOrder);
        return reactiveOrder == 0 ? string.Compare(Name, other.Name, StringComparison.Ordinal) : reactiveOrder;
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is SystemData other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(SystemData)}");
    }

    sealed class CleanupOrderRelationalComparer : IComparer<SystemData>
    {
        public int Compare(SystemData x, SystemData y)
        {
            var orderComparison = x.CleanupOrder.CompareTo(y.CleanupOrder);
            return orderComparison == 0 ? string.Compare(x.Name, y.Name, StringComparison.Ordinal) : orderComparison;
        }
    }

    public static IComparer<SystemData> CleanupOrderComparer { get; } = new CleanupOrderRelationalComparer();
}

[Flags]
public enum Execution
{
    PreUpdate = 1 << 0,
    PostUpdate = 1 << 1,

    PreLateUpdate = 1 << 2,
    PostLateUpdate = 1 << 3,

    PreFixedUpdate = 1 << 4,
    PostFixedUpdate = 1 << 5,
}

[Flags]
public enum SystemExecution
{
    PreUpdate = 1 << 0,
    PostUpdate = 1 << 1,

    PreLateUpdate = 1 << 2,
    PostLateUpdate = 1 << 3,

    PreFixedUpdate = 1 << 4,
    PostFixedUpdate = 1 << 5,

    Manual = 1 << 6,
}

public static class SystemExecutionExtensions
{
    public static bool HasFlagFast(this SystemExecution value, SystemExecution flag)
    {
        return (value & flag) != 0;
    }
}
