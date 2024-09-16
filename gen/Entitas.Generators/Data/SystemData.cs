using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.StringConstants;

namespace Entitas.Generators.Data;

public struct SystemData() : IClassDeclarationResolver, IAttributeResolver, IConstructorResolver, IFinalisable<SystemData>, IComparable<SystemData>
{
    public TypeData TypeData { get; private set; } = default;
    public string ValidLowerName { get; private set; } = null!;

    public bool IsInitializeSystem { get; private set; } = false;
    public bool IsReactiveSystem { get; private set; } = false;
    public bool IsExecuteSystem { get; private set; } = false;
    public bool IsCleanupSystem { get; private set; } = false;
    public bool IsTeardownSystem { get; private set; } = false;

    public SystemExecution ReactiveExecution { get; private set; } = SystemExecution.Manual;
    public SystemExecution ExecuteExecution { get; private set; } = SystemExecution.Manual;
    public SystemExecution CleanupExecution { get; private set; } = SystemExecution.Manual;
    public int ReactiveOrder { get; private set; } = 0;
    public int ExecuteOrder { get; private set; } = 0;
    public int CleanupOrder { get; private set; } = 0;
    public int InitializeOrder { get; private set; } = 0;
    public int TeardownOrder { get; private set; } = 0;

    public ImmutableArray<(TypeData component, EventType eventType)> TriggeredBy { get; private set; } = ImmutableArray<(TypeData, EventType)>.Empty;
    public ImmutableArray<TypeData> EntityIs { get; private set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> ManuallyAddedContexts { get; private set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<FieldData> ConstructorArguments { get; private set; } = ImmutableArray<FieldData>.Empty;

    public string? Namespace => TypeData.Namespace;
    public string FullName => TypeData.FullName;
    public string Name => TypeData.Name;

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken ct)
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
        ConstructorArguments = constructorMethod.Parameters.Select(x=> new FieldData(x.Type.ToDisplayString(), x.Name)).ToImmutableArray();
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
        var eventType = (EventType)(attributeData.ConstructorArguments[1].Value ?? EventType.Added);
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
        ManuallyAddedContexts = typedConstants.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ContextName)).ToImmutableArray();
        return true;
    }

    public SystemData? Finalise()
    {
        return this;
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

            if (ManuallyAddedContexts.Length > 0)
            {
                stringBuilder.AppendLine($"   {nameof(ManuallyAddedContexts)}:");
                foreach (var contexts in ManuallyAddedContexts)
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
