using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Entitas.Generators.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.StringConstants;

namespace Entitas.Generators.Data;

public struct ComponentData : IClassDeclarationResolver, IAttributeResolver, IFieldResolver
{
    public string? Namespace { get; private set; }
    public string FullName { get; private set; }
    public string Name { get; private set; }
    public string FullPrefix { get; private set; }
    public string Prefix { get; private set; }

    public List<FieldData> Fields { get; private set; } = new();

    public List<ComponentEventData> Events { get; private set; } = new();
    public List<int> ComponentAddedContexts { get; private set; } = new();
    public bool IsUnique { get; private set; }
    public CleanupMode CleanupMode { get; private set; }

    public ComponentData()
    {
        Namespace = null;
        FullName = null!;
        Name = null!;
        FullPrefix = null!;
        Prefix = null!;
        IsUnique = false;
        CleanupMode = CleanupMode.None;
    }

    public static bool SyntaxFilter(SyntaxNode node, CancellationToken ct)
        => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
           && classDeclaration.AttributeLists
               .SelectMany(x => x.Attributes)
               .Any(x => x is
               {
                   Name: IdentifierNameSyntax { Identifier.Text: ComponentName or ComponentAttributeName }
                   or QualifiedNameSyntax
                   {
                       Left: IdentifierNameSyntax { Identifier.Text: EntitasNamespaceName },
                       Right: IdentifierNameSyntax { Identifier.Text: ComponentName or ComponentAttributeName },
                   }
               });

    public bool TryResolveClassDeclaration(INamedTypeSymbol symbol)
    {
        Namespace = symbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : symbol.ContainingNamespace.ToDisplayString();

        FullName = symbol.ToDisplayString();
        Name = symbol.Name;

        FullPrefix = FullName.Replace(".", string.Empty).RemoveSuffix("Component");
        Prefix = Name.RemoveSuffix("Component");
        return true;
    }

    public bool TryResolveAttribute(AttributeData attributeData)
    {
        return attributeData switch
        {
            { AttributeClass.Name: UniqueAttributeName } => TryResolveUniqueAttribute(attributeData),
            { AttributeClass.Name: EventAttributeName } => TryResolveEventAttribute(attributeData),
            { AttributeClass.Name: CleanupAttributeName } => TryResolveCleanupAttribute(attributeData),
            { AttributeClass.Name: AddToContextAttributeName } => TryResolveAddToContextAttribute(attributeData),
            _ => true
        };
    }

    bool TryResolveAddToContextAttribute(AttributeData attributeData)
    {
        var contexts = attributeData.ConstructorArguments.FirstOrDefault().Values;
        foreach (var context in contexts)
        {
            if (context.Value == null)
                continue;
            ComponentAddedContexts.Add((int)context.Value);
        }

        return true;
    }

    bool TryResolveCleanupAttribute(AttributeData attributeData)
    {
        CleanupMode = (CleanupMode)(attributeData.ConstructorArguments.FirstOrDefault().Value ?? CleanupMode.None);
        return true;
    }

    bool TryResolveEventAttribute(AttributeData attributeData)
    {
        var eventData = new ComponentEventData();
        foreach (var constructorArgument in attributeData.NamedArguments)
        {
            switch (constructorArgument.Key)
            {
                case ConstructorNameEventTarget:
                    if (constructorArgument.Value.Value == null)
                        return false;
                    eventData.EventTarget = (EventTarget)constructorArgument.Value.Value;
                    break;
                case ConstructorNameEventType:
                    eventData.EventType = (EventType)(constructorArgument.Value.Value ?? EventType.Added);
                    break;
                case ConstructorNameOrder:
                    eventData.Order = (int?)constructorArgument.Value.Value ?? 0;
                    break;
            }
        }

        Events.Add(eventData);
        return true;
    }

    bool TryResolveUniqueAttribute(AttributeData attributeData)
    {
        IsUnique = true;
        return true;
    }

    public bool TryResolveField(IFieldSymbol fieldSymbol)
    {
        if (fieldSymbol.DeclaredAccessibility != Accessibility.Public)
            return true;

        var fieldData = new FieldData();

        fieldData.TryResolveField(fieldSymbol);
        fieldData.ResolveAttributes(fieldSymbol);

        Fields.Add(fieldData);

        return true;
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder("ComponentData: ")
            .AppendLine($"   {nameof(Namespace)}: {Namespace}")
            .AppendLine($"   {nameof(FullName)}: {FullName}")
            .AppendLine($"   {nameof(Name)}: {Name}")
            .AppendLine($"   {nameof(FullPrefix)}: {FullPrefix}")
            .AppendLine($"   {nameof(Prefix)}: {Prefix}")
            .AppendLine($"   {nameof(IsUnique)}: {IsUnique}")
            .AppendLine($"   {nameof(CleanupMode)}: {CleanupMode}");

        if (Events.Count > 0)
        {
            stringBuilder.AppendLine($"   {nameof(Events)}:");
            foreach (var eventData in Events)
            {
                stringBuilder.AppendLine($"      {eventData.ToString()}");
            }
        }

        if (ComponentAddedContexts.Count > 0)
        {
            stringBuilder.AppendLine($"   {nameof(ComponentAddedContexts)}:");
            foreach (var contexts in ComponentAddedContexts)
            {
                stringBuilder.AppendLine($"      {contexts}");
            }
        }

        stringBuilder.AppendLine($"   {nameof(Fields)}:");

        if (ComponentAddedContexts.Count > 0)
        {
            foreach (var fieldData in Fields)
            {
                stringBuilder.AppendLine($"      {fieldData.ToString()}");
            }
        }
        else
        {
            stringBuilder.AppendLine($"      This Component doesn't have any fields.");

        }

        return stringBuilder.ToString();
    }
}

public enum CleanupMode
{
    RemoveComponent,
    DestroyEntity,
    None,
}

public struct ComponentEventData
{
    public EventTarget EventTarget;
    public EventType EventType;
    public int Order;
}

public enum EventTarget
{
    Any,
    Self
}

public enum EventType
{
    Added,
    Removed
}
