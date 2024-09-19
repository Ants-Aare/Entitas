using System;
using Microsoft.CodeAnalysis.CSharp;

namespace Entitas.Generators.Utility
{
    public static class StringUtility
    {
        public static string FileNameHint(string? @namespace, string name)
        {
            return !string.IsNullOrEmpty(@namespace)
                ? $"{@namespace}.{name}.g.cs"
                : $"{name}.g.cs";
        }
        public static string CombinedNamespace(string? @namespace, string suffix)
        {
            return !string.IsNullOrEmpty(@namespace)
                ? $"{@namespace}.{suffix}"
                : suffix;
        }
        public static string ToValidLowerName(this string value)
        {
            var lowerFirst = char.ToLower(value[0]) + value.Substring(1);
            return SyntaxFacts.GetKeywordKind(lowerFirst) == SyntaxKind.None
                ? lowerFirst
                : $"@{lowerFirst}";
        }
        public static string NamespaceClassifier(this string? @namespace)
        {
            return !string.IsNullOrEmpty(@namespace)
                ? $"{@namespace}."
                : string.Empty;
        }

        public static string RemoveSuffix(this string str, string suffix)
        {
            return str.EndsWith(suffix, StringComparison.Ordinal)
                ? str.Substring(0, str.Length - suffix.Length)
                : str;
        }
        public static string? TryRemoveSuffix(this string str, string suffix)
        {
            return str.EndsWith(suffix, StringComparison.Ordinal)
                ? str.Substring(0, str.Length - suffix.Length)
                : null;
        }
    }
}
