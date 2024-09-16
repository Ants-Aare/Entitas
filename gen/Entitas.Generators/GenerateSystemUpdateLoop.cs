using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using AAA.SourceGenerators.Common;
using Entitas.Generators.Data;
using Microsoft.CodeAnalysis;

namespace Entitas.Generators;

public static class GenerateSystemUpdateLoop
{
    public static void GenerateSystemUpdateLoopOutput(SourceProductionContext context, ImmutableArray<SystemData> systemDatas)
    {
        if (systemDatas.Length == 0)
            return;

        context.CancellationToken.ThrowIfCancellationRequested();
        var stringBuilder = new StringBuilder();
        var targetNamespace = systemDatas.FirstOrDefault().TypeData.Namespace;
        try
        {
            stringBuilder.AppendGenerationWarning(nameof(GenerateSystemUpdateLoop));

            using (new NamespaceBuilder(stringBuilder, targetNamespace))
            {
                stringBuilder.AppendLine(GetContent(systemDatas));
            }

            using (new CommentBuilder(stringBuilder))
            {
                stringBuilder.AppendLine($"{systemDatas.Length}:");
                foreach (var systemData in systemDatas)
                {
                    stringBuilder.AppendLine($"IsReactiveSystem: {systemData.IsReactiveSystem}, IsExecuteSystem: {systemData.IsExecuteSystem}, IsCleanupSystem: {systemData.IsCleanupSystem}, {systemData.FullName} ");
                }
            }
        }
        catch (Exception e)
        {
            stringBuilder.AppendLine($"/*\nException occured while generating:\n{e}\n*/");
        }

        context.AddSource(Templates.FileNameHint(targetNamespace, "AddSystemsToUpdateLoop"), stringBuilder.ToString());
    }

    static string GetContent(ImmutableArray<SystemData> systemDatas)
    {
        var stringBuilder = new StringBuilder();
        AddSystemsForExecution(stringBuilder, systemDatas, SystemExecution.PreUpdate);
        AddSystemsForExecution(stringBuilder, systemDatas, SystemExecution.PostUpdate);
        AddSystemsForExecution(stringBuilder, systemDatas, SystemExecution.PreLateUpdate);
        AddSystemsForExecution(stringBuilder, systemDatas, SystemExecution.PostLateUpdate);
        AddSystemsForExecution(stringBuilder, systemDatas, SystemExecution.PreFixedUpdate);
        AddSystemsForExecution(stringBuilder, systemDatas, SystemExecution.PostFixedUpdate);

        return $$"""
                     public static partial class AddSystemsToUpdateLoop
                     {
                     #if UNITY_2020_1_OR_NEWER
                         [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
                     #else
                 		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
                     #endif
                         public static void Initialize()
                         {
                             {{stringBuilder}}
                         }
                     }
                 """;
    }

    public static void AddSystemsForExecution(StringBuilder sb, ImmutableArray<SystemData> systemDatas, SystemExecution execution)
    {
        var reactive = systemDatas.Where(x => x.IsReactiveSystem && x.ReactiveExecution.HasFlagFast(execution)).Select(x => (x.ReactiveOrder, $"\t\t\t{x.FullName}.UpdateReactiveSystems, // Order: {x.ReactiveOrder}"));
        var execute = systemDatas.Where(x => x.IsExecuteSystem && x.ExecuteExecution.HasFlagFast(execution)).Select(x => (x.ExecuteOrder, $"\t\t\t{x.FullName}.UpdateExecuteSystems, // Order: {x.ExecuteOrder}"));
        var cleanup = systemDatas.Where(x => x.IsCleanupSystem && x.CleanupExecution.HasFlagFast(execution)).Select(x => (x.CleanupOrder, $"\t\t\t{x.FullName}.Cleanup, // Order: {x.CleanupOrder}"));

        var systems = reactive.Concat(execute).ToList();
        systems.Sort(new SystemSortComparer());
        var cleanupSystems = cleanup.ToList();
        systems.Sort(new SystemSortComparer());

        if (cleanupSystems.Count > 0)
            systems.AddRange(cleanupSystems);

        if (systems.Count == 0)
            return;

        sb.Append('\n');
        sb.AppendLine($"\t\tEntitas.EntitasUpdateLoop.SetUpdateDelegatesForExecution(Entitas.Execution.{execution.ToString()}, new UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction[]{{ ");
        foreach ((int _, string systemCall) system in systems)
        {
            sb.AppendLine(system.systemCall);
        }

        sb.AppendLine("\t\t});");
    }

    class SystemSortComparer : IComparer<(int, string)>
    {
        public int Compare((int, string) x, (int, string) y)
        {
            var item1Comparison = x.Item1.CompareTo(y.Item1);
            return item1Comparison != 0
                ? item1Comparison
                : string.Compare(x.Item2, y.Item2, StringComparison.Ordinal);
        }
    }
}
