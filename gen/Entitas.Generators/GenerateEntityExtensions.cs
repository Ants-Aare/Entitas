using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateEntityExtensions
{
    public static void GenerateEntityExtensionsOutput(SourceProductionContext context, (ComponentData ComponentData, ContextData ContextData) value)
    {
        var componentData = value.ComponentData;
        var contextData = value.ContextData;

        var entityExtensions = new StringBuilder()
            .AppendGenerationWarning(nameof(GenerateEntityExtensions));

        try
        {
            // entityExtensions.AppendLine("public partial class GuguGaga{}");
            var methodSignature = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.TypeName} {field.ValidLowerName}"));
            // var methodSignatureWithLeadingComma = methodSignature == string.Empty ? string.Empty : $", {methodSignature}";
            var methodArguments = componentData.Fields.Length == 0 ? string.Empty : string.Join(", ", componentData.Fields.Select(static field => $"{field.ValidLowerName}"));
            var equalityComparer = componentData.Fields.Length == 0 ? string.Empty : string.Join("", componentData.Fields.Select(field => $" && this.{componentData.Name}.{field.Name} == {field.ValidLowerName}"));


            var entityExtensionsContent =
                $$"""
                  partial class {{contextData.Prefix}}Entity : {{componentData.Namespace.NamespaceClassifier()}}I{{componentData.Prefix}}Entity
                  {
                      {{componentData.FullName}} {{componentData.Name}};

                      public bool Has{{componentData.Prefix}}() => this.{{componentData.Name}} != null;

                      public {{componentData.FullName}} Get{{componentData.Prefix}}() => this.{{componentData.Name}};

                      public {{contextData.Prefix}}Entity Set{{componentData.Prefix}}({{methodSignature}})
                      {
                          if (!this.IsEnabled)
                          {
                              return this;
                          }

                          if (this.{{componentData.Name}} == null)
                          {
                              this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});
                              return this;
                          }

                          if (Has{{componentData.Prefix}}(){{equalityComparer}})
                          {
                              return this;
                          }

                          var previousComponent = this.{{componentData.Name}};
                          this.{{componentData.Name}} = {{componentData.FullName}}.CreateComponent({{methodArguments}});

                          {{componentData.FullName}}.DestroyComponent(previousComponent);
                          return this;
                      }

                      public {{contextData.Prefix}}Entity Remove{{componentData.Prefix}}()
                      {
                          if (!this.IsEnabled)
                          {
                              return this;
                          }

                          if (this.{{componentData.Name}} == null)
                          {
                              return this;
                          }

                          var previousComponent = this.{{componentData.Name}};
                          this.{{componentData.Name}} = null;

                          {{componentData.FullName}}.DestroyComponent(previousComponent);

                          return this;
                      }
                  }
                  """;

            using (new NamespaceBuilder(entityExtensions, contextData.Namespace))
            {
                entityExtensions.AppendLine(entityExtensionsContent);
            }
        }
        catch (Exception e)
        {
            entityExtensions.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, $"{contextData.Prefix}Entity{componentData.Prefix}Extensions"), entityExtensions.ToString());
    }
}
