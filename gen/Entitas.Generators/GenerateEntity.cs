using System;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Entitas.Generators.Utility;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public sealed class GenerateEntity
{
    public static void GenerateEntityOutput(SourceProductionContext context, ExtendedContextData data)
    {
        var contextData = data.ContextData;

        var stringBuilder = new StringBuilder();
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateEntity));
            using (new NamespaceBuilder(stringBuilder, contextData.Namespace))
            {
                stringBuilder.AppendLine(GetContent(data));
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(Templates.FileNameHint(contextData.Namespace, $"{contextData.Prefix}Entity"), stringBuilder.ToString());
    }

    static string GetContent(ExtendedContextData data)
    {
        var contextData = data.ContextData;
        var componentDatas = data.ComponentDatas;

        var entityComponents = componentDatas.Where(x => !x.IsUnique).ToArray();
        var destroyCalls = entityComponents.Length == 0
            ? string.Empty
            : string.Join("\n\n", entityComponents
                .Select(static c => $"{(c.IndexType == EntityIndexType.None ? string.Empty : $"\t\tContext.SetIndexed{c.Prefix}Entity(null, {c.GetVariableMethodArguments(c.Name)});\n")}\t\t{c.FullName}.DestroyComponent(this.{c.Name});\n\t\tthis.{c.Name} = null;"));

        var systemDatas = data.SystemDatas.Where(x=> x.IsReactiveSystem && x.NeedsCustomInterface()).ToList();
        var systemInterfaces = systemDatas.Count == 0 ? string.Empty : ',' + string.Join(", ", systemDatas.Select(x => $"{x.Namespace.NamespaceClassifier()}I{x.Name}Entity"));
        var featureInterfaces = contextData.Features.Length == 0 ? string.Empty : ',' + string.Join(", ", contextData.Features.Select(static x =>$"{x.NamespaceSpecifier}I{x.Name}Entity"));

        return $$"""
                 public sealed partial class {{contextData.Prefix}}Entity : Entitas.EntityBase, System.IEquatable<{{contextData.Prefix}}Entity>{{systemInterfaces}}{{featureInterfaces}}
                 {
                     public {{contextData.Name}} Context { get; private set; }
                     [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                     internal {{contextData.Prefix}}Entity(){}

                     [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
                     internal void InitializeEntity(int id, {{contextData.Name}} context)
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

                     public bool Equals({{contextData.Prefix}}Entity other)
                     {
                         if (ReferenceEquals(null, other)) return false;
                         if (ReferenceEquals(this, other)) return true;
                         return IsEnabled && other.IsEnabled && Id == other.Id && Context.Id == other.Context.Id;
                     }

                     public override bool Equals(object obj)
                     {
                         if (ReferenceEquals(null, obj)) return false;
                         if (ReferenceEquals(this, obj)) return true;
                         if (obj.GetType() != this.GetType()) return false;
                         return Equals(({{contextData.Prefix}}Entity)obj);
                     }

                     public override int GetHashCode()
                     {
                         return System.HashCode.Combine(Id, Context.Id);
                     }
                 }

                 public static class {{contextData.Prefix}}EntityEnumerableExtensions
                 {
                     public static {{contextData.Name}} GetContext(this System.Collections.Generic.IEnumerable<{{contextData.Prefix}}Entity> entities) => System.Linq.Enumerable.FirstOrDefault(entities)?.Context;
                 }
                 """;
    }
}
