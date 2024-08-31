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

public struct SystemData() : IClassDeclarationResolver, IAttributeResolver, IFinalisable<SystemData>
{
    public string? Namespace => TypeData.Namespace;
    public string FullName => TypeData.FullName;
    public string Name => TypeData.Name;
    public string ValidLowerName { get; private set; } = null!;

    public TypeData TypeData { get; private set; } = default;
    public bool IsInitializeSystem { get; private set; } = false;

    public bool IsReactiveSystem { get; private set; } = false;
    public bool IsExecuteSystem { get; private set; } = false;

    public Execution ReactiveExecution { get; private set; } = Execution.Manual;
    public Execution ExecuteExecution { get; private set; } = Execution.Manual;
    public int ReactiveOrder { get; private set; } = 0;
    public int ExecuteOrder { get; private set; } = 0;

    public ImmutableArray<(TypeData component, EventType eventType)> TriggeredBy { get; private set; } = ImmutableArray<(TypeData, EventType)>.Empty;
    public ImmutableArray<TypeData> EntityIs { get; private set; } = ImmutableArray<TypeData>.Empty;
    public ImmutableArray<TypeData> ManuallyAddedContexts { get; private set; } = ImmutableArray<TypeData>.Empty;



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

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: ReactiveSystemAttributeName } => TryResolveReactiveSystemAttribute(attributeData),
            { AttributeClass.Name: InitializeSystemAttributeName } => TryResolveInitializeSystemAttributeAttribute(),
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
        ReactiveExecution = (Execution)(attributeData.ConstructorArguments[0].Value ?? Execution.Manual);
        ReactiveOrder = (int)(attributeData.ConstructorArguments[0].Value ?? 0);
        return true;
    }

    bool TryResolveInitializeSystemAttributeAttribute()
    {
        IsInitializeSystem = true;
        return true;
    }

    bool TryResolveExecuteSystemAttribute(AttributeData attributeData)
    {
        IsExecuteSystem = true;
        ExecuteExecution = (Execution)(attributeData.ConstructorArguments[0].Value ?? Execution.Manual);
        ExecuteOrder = (int)(attributeData.ConstructorArguments[0].Value ?? 0);
        return true;
    }

    bool TryResolveEntityIsAttribute(AttributeData attributeData)
    {
        EntityIs = attributeData.ConstructorArguments[0].Values
            .Where(x=> x.Value is INamedTypeSymbol)
            .Select(x=> TypeData.Create((INamedTypeSymbol)x.Value!, ComponentName))
            .ToImmutableArray();
        return true;
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var typedConstants = attributeData.ConstructorArguments[0].Values;
        ManuallyAddedContexts = typedConstants.Select(x => TypeData.Create((INamedTypeSymbol)x.Value!, ContextName)).ToImmutableArray();
        return true;
    }

    public SystemData Finalise()
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
                .AppendLine($"   {nameof(IsInitializeSystem)}: {IsInitializeSystem}")
                .AppendLine($"   {nameof(IsReactiveSystem)}: {IsReactiveSystem} {ReactiveExecution} {ReactiveOrder}")
                .AppendLine($"   {nameof(IsExecuteSystem)}: {IsExecuteSystem} {ExecuteExecution} {ExecuteOrder}");

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
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        return stringBuilder.ToString();
    }
}

public enum Execution
{
    PreUpdate,
    Update,
    PostUpdate,

    PreLateUpdate,
    LateUpdate,
    PostLateUpdate,

    PreFixedUpdate,
    FixedUpdate,
    PostFixedUpdate,

    Manual,
}
