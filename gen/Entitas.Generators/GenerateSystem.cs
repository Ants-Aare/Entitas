using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateSystem
{
    public static void GenerateSystemOutput(SourceProductionContext context, SystemWithComponents data)
    {
        var systemData = data.SystemData;
        var componentDatas = data.ComponentDatas;
        var stringBuilder = new StringBuilder();
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var hasMultipleConstraints = systemData.HasMultipleConstraints();

            var entityType = systemData.GetEntityType(componentDatas.First(x => x.Name == systemData.TriggeredBy[0].component));

            var content = $$"""
                            public sealed partial class {{systemData.Name}} : Entitas.IReactiveSystem<{{entityType}}>
                            {
                                static readonly System.Collections.Generic.HashSet<{{systemData.Name}}> Instances = new();
                                readonly System.Collections.Generic.HashSet<{{entityType}}> _collector = new ();
                                readonly System.Collections.Generic.List<{{entityType}}> _buffer = new ();
                                public static void UpdateSystems()
                                {
                                    foreach (var systemInstance in Instances)
                                        systemInstance.UpdateSystem();
                                }
                                public void UpdateSystem()
                                {
                                    if (_collector.Count == 0)
                                        return;

                                    foreach (var entity in _collector)
                                        if (Filter(entity))
                                            _buffer.Add(entity);

                                    _collector.Clear();

                                    if (_buffer.Count == 0)
                                        return;

                                    try
                                    {
                                        Execute(_buffer);
                                    }
                                    finally
                                    {
                                        _buffer.Clear();
                                    }
                                }

                                public void Enable() =>Instances.Add(this);

                                public void Disable() =>Instances.Remove(this);

                                public void OnEntityTriggered({{entityType}} entity)
                                {
                                    _collector.Add(entity);
                                }

                                ~{{systemData.Name}}()=>Instances.Remove(this);
                            }
                            """;


            stringBuilder.AppendGenerationWarning(nameof(GenerateSystem));
            using (new NamespaceBuilder(stringBuilder, systemData.Namespace))
            {
                stringBuilder.AppendLine(content);

                if (hasMultipleConstraints)
                {
                    var enumerable = componentDatas.Select(x => $"{x.Namespace.NamespaceClassifier()}I{x.Prefix}Entity");
                    var interfaceDefinition = $"public interface I{systemData.Name}Entity : {string.Join(", ", enumerable)}{{ }}";
                    stringBuilder.AppendLine(interfaceDefinition);
                }
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine(systemData.ToString());
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine(e.ToString());
        }

        context.AddSource(Templates.FileNameHint(systemData.Namespace, systemData.Name), stringBuilder.ToString());
    }
}
