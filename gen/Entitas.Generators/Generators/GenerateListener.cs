using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators.Generators;

public static class GenerateListener
{
    public static void GenerateListenerOutput(SourceProductionContext context, ExtendedListenerData data)
    {
        var listenerData = data.ListenerData;
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            // var entityType = listenerData.GetEntityType();
            // var contextType = listenerData.GetContextType();

            stringBuilder.AppendGenerationWarning(nameof(GenerateListener));
            using (new NamespaceBuilder(stringBuilder, listenerData.Namespace))
            {
                stringBuilder.AppendLine(GetContent(data));
                foreach (var eventData in listenerData.Events)
                {
                    AppendEventContent(stringBuilder, data, eventData);
                }

                stringBuilder.Append('}');
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(listenerData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(StringUtility.FileNameHint(listenerData.Namespace, listenerData.Name), stringBuilder.ToString());
    }

    static string GetContent(ExtendedListenerData data/*, string entityType, string contextType*/)
    {
        var listenerData = data.ListenerData;
        var interfaces = new StringBuilder();
        for (var i = 0; i < listenerData.Events.Length; i++)
        {
            var listenerEvent = listenerData.Events[i];
            if (i != 0)
                interfaces.Append(',');
            interfaces.Append(listenerEvent.Component.NamespaceSpecifier)
                .Append('I')
                .Append(listenerEvent.Component.Prefix)
                .Append(listenerEvent.ComponentEvent.ToString())
                .Append("Listener");
        }

        var startListeningCalls = new StringBuilder();
        var stopListeningCalls = new StringBuilder();

        // there is a suuuper weird error for the generated switch statement: The switch case is unreachable. It has already been handled by a previous case or it is impossible to match.
        // so I will be using if else statements here
        for (var i = 0; i < data.ContextDatas.Length; i++)
        {
            var context = data.ContextDatas[i];
            if (i == 0)
            {
                startListeningCalls.Append("\n\t\tif(entity is ").Append(context.FullPrefix).Append("Entity ").Append(context.Prefix).Append("Entity){");
                stopListeningCalls.Append("\n\t\tif(entity is ").Append(context.FullPrefix).Append("Entity ").Append(context.Prefix).Append("Entity){");
            }
            else
            {
                startListeningCalls.Append("\n\t\telse if(entity is ").Append(context.FullPrefix).Append("Entity ").Append(context.Prefix).Append("Entity){");
                stopListeningCalls.Append("\n\t\telse if(entity is ").Append(context.FullPrefix).Append("Entity ").Append(context.Prefix).Append("Entity){");
            }

            foreach (var eventData in listenerData.Events.Where(x => context.Components.Any(y => y == x.Component) || data.ComponentDatas.Any(y => y.Contexts.Any(c => c == context.TypeData))))
            {
                // startListeningCalls.AppendLine("                case MatchOne.Game.GameEntity GameEntity: GameEntity.Context.zBoardAddedListener = this; break;");
                startListeningCalls.Append("\n\t\t\t")
                    .Append(context.Prefix)
                    .Append("Entity");
                if (eventData.ListenTarget == ListenTarget.Context)
                    startListeningCalls.Append(".Context");
                startListeningCalls.Append(".z")
                    .Append(eventData.Component.Prefix)
                    .Append(eventData.ComponentEvent)
                    .Append("Listener");
                startListeningCalls.Append(eventData.AllowMultipleListeners ? "s.Add(this);" : " = this;");

                startListeningCalls.Append("\n\t\t\t")
                    .Append(context.Prefix)
                    .Append("Entity");
                if (eventData.ListenTarget == ListenTarget.Context)
                    startListeningCalls.Append(".Context");
                startListeningCalls.Append(".z")
                    .Append(eventData.Component.Prefix)
                    .Append(eventData.ComponentEvent)
                    .Append("Listener");
                startListeningCalls.Append(eventData.AllowMultipleListeners ? "s.Remove(this);" : " = null;");
            }

            startListeningCalls.Append('}');
            stopListeningCalls.Append('}');
        }

        // var startListeningCalls = new StringBuilder().AppendJoin("\n\t\t", listenerData.Events, x => $"StartListeningTo{x.Component.Prefix}{x.ComponentEvent}(({x.Component.NamespaceSpecifier}I{x.Component.Prefix}Entity)entity);");
        // var stopListeningCalls = new StringBuilder().AppendJoin("\n\t\t", listenerData.Events, x => $"StopListeningTo{x.Component.Prefix}{x.ComponentEvent}(({x.Component.NamespaceSpecifier}I{x.Component.Prefix}Entity)entity);");
        return $$"""
                 partial class {{listenerData.Name}} : {{interfaces}}
                 {
                     public void StartListening(Entitas.IEntity entity)
                     {{{startListeningCalls}}
                     }
                     public void StopListening(Entitas.IEntity entity)
                     {{{stopListeningCalls}}
                     }
                 """;
    }

    static void AppendEventContent(StringBuilder stringBuilder, ExtendedListenerData data, EventData eventData)
    {
        var contexts = data.ContextDatas.Where(context => context.Components.Any(x => x == eventData.Component)
                                                          || data.ComponentDatas.Any(y => y.Contexts.Any(z => z == context.TypeData)))
            .ToList();

        // var listenerData = data.ListenerData;
        var eventName = eventData.Component.Prefix + eventData.ComponentEvent;

        // var component = data.ComponentDatas.FirstOrDefault(x => x.TypeData == eventData.Component);
        // string methodCall = eventData switch
        // {
        //     { ComponentEvent: ComponentEvent.Removed, ListenTarget: ListenTarget.Context } => $"listener.On{eventName}(listener.Entity.GetI{eventData.Component.Prefix}Context());",
        //     { ComponentEvent: ComponentEvent.Removed, ListenTarget: ListenTarget.Entity } => $"listener.On{eventName}(listener.Entity);",
        //     { ListenTarget: ListenTarget.Context } => $"listener.On{eventName}(listener.Entity.GetI{eventData.Component.Prefix}Context(){component.GetVariableMethodArgumentsLeadingComma()});",
        //     _ => $"listener.On{eventName}(listener.Entity{component.GetVariableMethodArgumentsLeadingComma()});",
        // };

        stringBuilder.AppendLine(
            $$"""
              public void StartListeningTo{{eventName}}({{eventData.Component.NamespaceSpecifier}}I{{eventData.Component.Prefix}}Entity entity)
              {
                  switch(entity)
                  {
              """);
        foreach (var contextData in contexts)
        {
            stringBuilder.Append("\t\tcase ")
                .Append(contextData.FullPrefix)
                .Append("Entity ")
                .Append(contextData.Prefix)
                .Append("Entity: ")
                .Append(contextData.Prefix)
                .Append("Entity");
            if (eventData.ListenTarget == ListenTarget.Context)
                stringBuilder.Append(".Context");
            stringBuilder.Append(".z")
                .Append(eventName)
                .Append("Listener");
            stringBuilder.AppendLine(eventData.AllowMultipleListeners ? "s.Add(this); break;" : " = this; break;");
        }

        stringBuilder.AppendLine("}}");

        stringBuilder.AppendLine(
            $$"""
              public void StopListeningTo{{eventName}}({{eventData.Component.NamespaceSpecifier}}I{{eventData.Component.Prefix}}Entity entity)
              {
                  switch(entity)
                  {
              """);
        foreach (var contextData in contexts)
        {
            stringBuilder.Append("\t\tcase ")
                .Append(contextData.FullPrefix)
                .Append("Entity ")
                .Append(contextData.Prefix)
                .Append("Entity: ")
                .Append(contextData.Prefix)
                .Append("Entity");
            if (eventData.ListenTarget == ListenTarget.Context)
                stringBuilder.Append(".Context");
            stringBuilder.Append(".z")
                .Append(eventName)
                .Append("Listener");
            stringBuilder.AppendLine(eventData.AllowMultipleListeners ? "s.Remove(this); break;" : " = null; break;");
        }
        stringBuilder.AppendLine("}}");

    }
}
