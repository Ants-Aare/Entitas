using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

public static class GenerateFeature
{
    public static void GenerateFeatureOutput(SourceProductionContext context, FeatureData featureData)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateFeature));

            using (new NamespaceBuilder(stringBuilder, featureData.Namespace))
            {
                var entities = featureData.Components.Length == 0 ? string.Empty : ':' + string.Join(", ", featureData.Components.Select(x => $"{x.NamespaceSpecifier}I{x.Prefix}Entity"));
                var contexts = featureData.Components.Length == 0 ? string.Empty : ':' + string.Join(", ", featureData.Components.Select(x => $"{x.NamespaceSpecifier}I{x.Prefix}Context"));
                stringBuilder.AppendLine($"public sealed partial class {featureData.Name}{{}}\npublic interface I{featureData.Name}Context{contexts}{{}}\npublic interface I{featureData.Name}Entity{entities}{{}}");
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(featureData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(StringUtility.FileNameHint(featureData.Namespace, featureData.Name), stringBuilder.ToString());
    }
}
