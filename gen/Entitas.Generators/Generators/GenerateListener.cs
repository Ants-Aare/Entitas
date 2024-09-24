using System;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

public static class GenerateListener
{
    public static void GenerateListenerOutput(SourceProductionContext context, ListenerData data)
    {
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            stringBuilder.AppendGenerationWarning(nameof(GenerateListener));
            using (new NamespaceBuilder(stringBuilder, data.Namespace))
            {
                stringBuilder.AppendLine(GetContent(data));
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(data.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(StringUtility.FileNameHint(data.Namespace, data.Name), stringBuilder.ToString());
    }

    static string GetContent(ListenerData listenerData)
    {
        var interfaces = new StringBuilder();

        for (var i = 0; i < listenerData.Events.Length; i++)
        {
            var listenerEvent = listenerData.Events[i];
            if (i != 0)
                interfaces.Append(',');
            interfaces.Append(listenerEvent.Type.NamespaceSpecifier)
                .Append('I')
                .Append(listenerEvent.Type.Prefix)
                .Append(listenerEvent.ComponentEvent.ToString())
                .Append("Listener");
        }

        return $$"""
                 partial class {{listenerData.Name}} : {{interfaces}}
                 {
                     public void StartListening(Entitas.IEntity entity)
                     {
                     }
                     public void StopListening(Entitas.IEntity entity)
                     {
                     }
                 }
                 """;
    }
}
