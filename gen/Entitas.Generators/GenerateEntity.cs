using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateEntity
{
    public static void GenerateEntityOutput(SourceProductionContext context, ContextWithComponents value)
    {
        var contextData = value.ContextData;
        var componentDatas = value.ComponentDatas;

        var entityComponents = componentDatas.Where(x => !x.IsUnique).ToArray();
        // var interfaceImplementations = componentDatas.Length == 0 ? string.Empty : string.Join("", componentDatas.Select(static component => $", {component.Namespace.NamespaceClassifier()}I{component.Prefix}Entity"));
        // var fieldDeclarations = componentDatas.Length == 0 ? string.Empty : string.Join("\n", componentDatas.Select(static component => $"{component.FullName} {component.Name};"));
        var destroyCalls = entityComponents.Length == 0 ? string.Empty : string.Join("\n", entityComponents.Select(static component => $"{component.FullName}.DestroyComponent(this.{component.Name}); \n this.{component.Name} = null;"));

        var stringBuilder = new StringBuilder();
        try
        {
            var content = $$"""
                            public sealed partial class {{contextData.Prefix}}Entity : Entitas.EntityBase
                            {
                                public {{contextData.Name}} Context { get; private set; }
                                internal {{contextData.Prefix}}Entity(){}


                                public void InitializeEntity(int id, {{contextData.Name}} context)
                                {
                                    Id = id;
                                    Context = context;
                                    IsEnabled = true;
                                }
                                public void DestroyImmediate()
                                {
                            {{destroyCalls}}
                                    Context.ReturnEntity(this);
                                    Context = null;
                                    IsEnabled = false;
                                    Id = -1;
                                }
                            }
                            """;

            stringBuilder.AppendGenerationWarning(nameof(GenerateEntity));

            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(content);
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, $"{contextData.Prefix}Entity"), stringBuilder.ToString());
    }
}
