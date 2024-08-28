using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateContext
{
    public static void GenerateContextOutput(SourceProductionContext context, ContextWithComponents value)
    {
        var contextData = value.ContextData;
        var componentDatas = value.ComponentDatas;
        var strings = componentDatas.Length == 0 ? string.Empty : string.Join(", ", componentDatas.Select(static component => $"\"{component.Name}\""));
        var types = componentDatas.Length == 0 ? string.Empty : string.Join(", ", componentDatas.Select(static component => $"typeof({component.FullName})"));

        // var uniqueComponents = componentDatas.Where(x => x.IsUnique).ToArray();
        // var fieldDeclarations = uniqueComponents.Length == 0 ? string.Empty : string.Join("\n", uniqueComponents.Select(static component => $"{component.FullName} {component.Name};"));

        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
                            public sealed partial class {{contextData.Name}} : Entitas.ContextBase
                            {
                                public const string Name = "{{contextData.Name}}";
                                public static readonly Entitas.ContextInfo ContextInfo = new Entitas.ContextInfo("{{contextData.Name}}",
                                    new[]{{{strings}}},
                                    new[]{{{types}}});

                                static int _creationIndex;
                                static readonly System.Collections.Generic.Stack<{{contextData.Prefix}}Entity> EntityPool;
                                static readonly System.Collections.Generic.Stack<{{contextData.Name}}> ContextPool;

                                System.Collections.Generic.Dictionary<int, {{contextData.Prefix}}Entity> _enabledEntities = new();
                                {{contextData.Prefix}}Entity contextEntity;

                                private {{contextData.Name}}()
                                {
                                    Id = _creationIndex++;
                                    IsEnabled = true;
                                    contextEntity = CreateEntity();
                                }

                                public static {{contextData.Name}} CreateContext()
                                {
                                    var context = new {{contextData.Name}}()
                                    {
                                    };

                                    return context;
                                }

                                public void DestroyContext()
                                {
                                }

                                public {{contextData.Prefix}}Entity CreateEntity()
                                {
                                    var entity = EntityPool.Count <= 0
                                        ? new {{contextData.Prefix}}Entity()
                                        : EntityPool.Pop();

                                    entity.InitializeEntity(_creationIndex++, this);
                                    _enabledEntities.Add(_creationIndex, entity);
                                    return entity;
                                }

                                internal void ReturnEntity({{contextData.Prefix}}Entity entity)
                                {
                                    if (contextEntity == entity)
                                        contextEntity = CreateEntity();
                                    _enabledEntities.Remove(entity.Id);
                                    EntityPool.Push(entity);
                                }
                            }
                            """;

            stringBuilder.AppendGenerationWarning(nameof(GenerateContext));

            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(content);
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(contextData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, contextData.Name), stringBuilder.ToString());
    }
}
