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

public struct SystemData : IClassDeclarationResolver, IAttributeResolver, IFinalisable<SystemData>
{
    public string? Namespace { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }
    public string ValidLowerName { get; private set; }

    public bool IsInitializeSystem { get; private set; }

    public bool IsReactiveSystem { get; private set; }

    public Execution ReactiveExecution { get; private set; }
    public int ReactiveOrder { get; private set; }
    public ImmutableArray<(string component, EventType eventType)> TriggeredBy { get; private set; } = ImmutableArray<(string, EventType)>.Empty;
    public ImmutableArray<string> EntityIs { get; private set; } = ImmutableArray<string>.Empty;

    public bool IsExecuteSystem { get; private set; }
    public Execution ExecuteExecution { get; private set; }
    public int ExecuteOrder { get; private set; }

    public ImmutableArray<string> ManuallyAddedContexts { get; private set; } = ImmutableArray<string>.Empty;

    public SystemData()
    {
        Namespace = null!;
        FullName = null!;
        Name = null!;
        ValidLowerName = null!;
        IsInitializeSystem = false;
        IsReactiveSystem = false;
        ReactiveExecution = Execution.Manual;
        ReactiveOrder = 0;
        IsExecuteSystem = false;
        ExecuteExecution = Execution.Manual;
        ExecuteOrder = 0;
    }

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
        Namespace = symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();

        FullName = symbol.ToDisplayString();
        Name = symbol.Name;
        ValidLowerName = Name.ToValidLowerName();
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: ReactiveSystemAttributeName } => TryResolveReactiveSystemAttribute(attributeData),
            { AttributeClass.Name: InitializeSystemAttributeName } => TryResolveInitializeSystemAttributeAttribute(attributeData),
            { AttributeClass.Name: ExecuteSystemAttributeName } => TryResolveExecuteSystemAttribute(attributeData),
            { AttributeClass.Name: EntityIsAttributeName } => TryResolveEntityIsAttribute(attributeData),
            { AttributeClass.Name: AddToContextAttributeName } => TryResolveAddToContextAttribute(attributeData),
            { AttributeClass.Name: TriggeredByAttributeName } => TryResolveTriggeredByAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveTriggeredByAttribute(AttributeData attributeData)
    {
        var componentPrefix = ((string)attributeData.ConstructorArguments[0].Value!);
        var eventType = (EventType)(attributeData.ConstructorArguments[1].Value ?? EventType.Added);
        TriggeredBy = TriggeredBy.Add((componentPrefix, eventType));
        return true;
    }

    bool TryResolveReactiveSystemAttribute(AttributeData attributeData)
    {
        IsReactiveSystem = true;
        ReactiveExecution = (Execution)(attributeData.ConstructorArguments[0].Value ?? Execution.Manual);
        ReactiveOrder = (int)(attributeData.ConstructorArguments[0].Value ?? 0);
        return true;
    }

    bool TryResolveInitializeSystemAttributeAttribute(AttributeData attributeData)
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
        EntityIs = attributeData.ConstructorArguments[0].Values.Select(x => (string)x.Value!).ToImmutableArray();
        return true;
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var typedConstants = attributeData.ConstructorArguments[0].Values;
        ManuallyAddedContexts = typedConstants.Select(x => (string)x.Value!).ToImmutableArray();
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
