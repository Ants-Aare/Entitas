using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Entitas.Generators.Templates;

namespace Entitas.Generators
{
    [Generator(LanguageNames.CSharp)]
    public class ComponentGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            initContext.RegisterSourceOutput(initContext.SyntaxProvider
                .CreateSyntaxProvider(SyntacticComponentPredicate, SemanticComponentTransform)
                .Where(component => component is not null), (spc, component) => Execute(spc, component!.Value));
        }

        static bool SyntacticComponentPredicate(SyntaxNode node, CancellationToken cancellationToken)
        {
            return node is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } candidate
                   && !candidate.Modifiers.Any(SyntaxKind.PublicKeyword)
                   && !candidate.Modifiers.Any(SyntaxKind.StaticKeyword)
                   && !candidate.Modifiers.Any(SyntaxKind.SealedKeyword)
                   && candidate.Modifiers.Any(SyntaxKind.PartialKeyword);
        }

        static ComponentDeclaration? SemanticComponentTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var candidate = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(candidate, cancellationToken);
            if (symbol is null)
                return null;

            // Todo: Emit diagnostics when interface is not found
            var interfaceType = context.SemanticModel.Compilation.GetTypeByMetadataName("Entitas.IComponent");
            if (interfaceType is null)
                return null;

            var isComponent = symbol.Interfaces.Any(i => i.OriginalDefinition.Equals(interfaceType, SymbolEqualityComparer.Default));
            if (!isComponent)
                return null;

            return new ComponentDeclaration(symbol, context, cancellationToken);
        }

        static void Execute(SourceProductionContext spc, ComponentDeclaration component)
        {
            ComponentIndex(spc, component);
            EntityExtension(spc, component);
        }

        static void ComponentIndex(SourceProductionContext spc, ComponentDeclaration component)
        {
            foreach (var context in component.Contexts)
            {
                var className = $"{component.FullComponentPrefix}ComponentIndex";
                spc.AddSource(
                    GeneratedPath($"{context}.{className}"),
                    GeneratedFileHeader(GeneratorSource(nameof(ComponentIndex))) +
                    NamespaceDeclaration(context,
                        $$"""
                        public static class {{className}}
                        {
                            public static ComponentIndex Value;
                        }

                        """));
            }
        }

        static void EntityExtension(SourceProductionContext spc, ComponentDeclaration component)
        {
            foreach (var context in component.Contexts)
            {
                var className = $"{component.FullComponentPrefix}EntityExtension";
                var index = $"{component.FullComponentPrefix}ComponentIndex.Value";
                if (component.Members.Length > 0)
                {
                    spc.AddSource(GeneratedPath($"{context}.{className}"),
                        GeneratedFileHeader(GeneratorSource(nameof(EntityExtension))) +
                        NamespaceDeclaration(context,
                            $$"""
                        public static class {{className}}
                        {
                            public static bool Has{{component.ComponentPrefix}}(this {{context}}.Entity entity)
                            {
                                return entity.HasComponent({{index}});
                            }

                            public static {{context}}.Entity Add{{component.ComponentPrefix}}(this {{context}}.Entity entity, {{ComponentMethodArgs(component)}})
                            {
                                var index = {{index}};
                                var component = ({{component.FullName}})entity.CreateComponent(index, typeof({{component.FullName}}));
                        {{ComponentValueAssignments(component)}}
                                entity.AddComponent(index, component);
                                return entity;
                            }

                            public static {{context}}.Entity Replace{{component.ComponentPrefix}}(this {{context}}.Entity entity, {{ComponentMethodArgs(component)}})
                            {
                                var index = {{index}};
                                var component = ({{component.FullName}})entity.CreateComponent(index, typeof({{component.FullName}}));
                        {{ComponentValueAssignments(component)}}
                                entity.ReplaceComponent(index, component);
                                return entity;
                            }

                            public static {{context}}.Entity Remove{{component.ComponentPrefix}}(this {{context}}.Entity entity)
                            {
                                entity.RemoveComponent({{index}});
                                return entity;
                            }
                        }

                        """));

                    static string ComponentMethodArgs(ComponentDeclaration component)
                    {
                        return string.Join(", ", component.Members.Select(member => $"{member.Type} {member.Name.ToValidLowerFirst()}"));
                    }

                    static string ComponentValueAssignments(ComponentDeclaration component)
                    {
                        return string.Join("\n", component.Members.Select(member =>
                            $$"""
                                    component.{{member.Name}} = {{member.Name.ToValidLowerFirst()}};
                            """));
                    }
                }
                else
                {
                    spc.AddSource(GeneratedPath($"{context}.{className}"),
                        GeneratedFileHeader(GeneratorSource(nameof(EntityExtension))) +
                        NamespaceDeclaration(context,
                            $$"""
                        public static class {{className}}
                        {
                            static readonly {{component.FullName}} Single{{component.Name}} = new {{component.FullName}}();

                            public static bool Has{{component.ComponentPrefix}}(this {{context}}.Entity entity)
                            {
                                return entity.HasComponent({{index}});
                            }

                            public static {{context}}.Entity Add{{component.ComponentPrefix}}(this {{context}}.Entity entity)
                            {
                                entity.AddComponent({{index}}, Single{{component.Name}});
                                return entity;
                            }

                            public static {{context}}.Entity Replace{{component.ComponentPrefix}}(this {{context}}.Entity entity)
                            {
                                entity.ReplaceComponent({{index}}, Single{{component.Name}});
                                return entity;
                            }

                            public static {{context}}.Entity Remove{{component.ComponentPrefix}}(this {{context}}.Entity entity)
                            {
                                entity.RemoveComponent({{index}});
                                return entity;
                            }
                        }

                        """));
                }
            }
        }

        static string GeneratorSource(string source)
        {
            return $"{typeof(ComponentGenerator).FullName}.{source}";
        }

        public readonly struct ComponentDeclaration : IEquatable<ComponentDeclaration>
        {
            /// When: MyApp.SomeComponent
            /// Then: MyApp
            public readonly string? Namespace;

            /// When: MyApp.SomeComponent
            /// Then: MyApp.SomeComponent
            public readonly string FullName;

            /// When: MyApp.SomeComponent
            /// Then: SomeComponent
            public readonly string Name;

            public readonly ImmutableArray<MemberDeclaration> Members;
            public readonly ImmutableArray<string> Contexts;

            public readonly Location Location;

            /// When: MyApp.SomeComponent
            /// Then: MyAppSome
            public readonly string FullComponentPrefix;

            /// When: MyApp.SomeComponent
            /// Then: Some
            public readonly string ComponentPrefix;

            public ComponentDeclaration(INamedTypeSymbol symbol, GeneratorSyntaxContext context, CancellationToken cancellationToken)
            {
                Namespace = !symbol.ContainingNamespace.IsGlobalNamespace ? symbol.ContainingNamespace.ToDisplayString() : null;
                FullName = symbol.ToDisplayString();
                Name = symbol.Name;

                Members = symbol.GetMembers()
                    // TODO: also filter static members
                    .Where(member => member.DeclaredAccessibility == Accessibility.Public)
                    .Select<ISymbol, MemberDeclaration?>(member =>
                    {
                        var memberType = member switch
                        {
                            IFieldSymbol field => field.Type,
                            IPropertySymbol property => property.Type,
                            _ => null
                        };

                        if (memberType is null)
                            return null;

                        return new MemberDeclaration(
                            memberType.ToDisplayString(),
                            member.Name);
                    })
                    .Where(member => member is not null)
                    .Select(member => member!.Value)
                    .ToImmutableArray();

                // TODO: remove, just for testing
                Contexts = ImmutableArray.Create<string>("MyApp.Main");

                Location = symbol.Locations.FirstOrDefault() ?? Location.None;

                FullComponentPrefix = FullName.Replace(".", string.Empty).RemoveSuffix("Component");
                ComponentPrefix = Name.RemoveSuffix("Component");
            }

            public bool Equals(ComponentDeclaration other) =>
                Namespace == other.Namespace &&
                FullName == other.FullName &&
                Name == other.Name &&
                Members.SequenceEqual(other.Members) &&
                Contexts.SequenceEqual(other.Contexts);

            public override bool Equals(object? obj) => obj is ComponentDeclaration other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Namespace, FullName, Name, Members, Contexts);
        }

        public readonly struct MemberDeclaration : IEquatable<MemberDeclaration>
        {
            public readonly string Type;
            public readonly string Name;

            public MemberDeclaration(string type, string name)
            {
                Type = type;
                Name = name;
            }

            public bool Equals(MemberDeclaration other) => Type == other.Type && Name == other.Name;
            public override bool Equals(object? obj) => obj is MemberDeclaration other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Type, Name);
        }
    }
}
