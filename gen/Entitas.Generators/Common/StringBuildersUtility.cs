using System;
using System.Collections.Generic;
using System.Text;

namespace AAA.SourceGenerators.Common;

public static class StringBuildersUtility
{
    public static StringBuilder AppendGenerationWarning(this StringBuilder builder, string generatorName)
    {
        builder.Append("// <auto-generated>\n//     This code was generated by ");
        builder.Append(generatorName);
        builder.AppendLine("\n// </auto-generated>");
        return builder;
    }

    public static StringBuilder AppendMethodSignature(this StringBuilder stringBuilder, IEnumerator<(string type, string argName)> arguments)
    {
        stringBuilder.Append('(');
        arguments.MoveNext();
        stringBuilder.Append(arguments.Current.type).Append(' ').Append(arguments.Current.argName);
        while (arguments.MoveNext())
        {
            stringBuilder.Append(',').Append(arguments.Current.type).Append(' ').Append(arguments.Current.argName);
        }

        stringBuilder.Append(")\n");
        return stringBuilder;
    }
}

public abstract class StringBuilderGenerationBase : IDisposable
{
    protected readonly StringBuilder StringBuilder;

    protected StringBuilderGenerationBase(StringBuilder stringBuilder)
    {
        StringBuilder = stringBuilder;
    }

    public void Dispose()
    {
        AppendBottom();
    }

    protected abstract void AppendBottom();
}

public class NamespaceBuilder : StringBuilderGenerationBase
{
    private readonly string? _targetNamespace;

    public NamespaceBuilder(StringBuilder stringBuilder, string? targetNamespace) : base(stringBuilder)
    {
        _targetNamespace = targetNamespace;
        if (_targetNamespace != null)
            StringBuilder.AppendLine($"namespace {_targetNamespace}\n{{");
    }

    protected override void AppendBottom()
    {
        if (_targetNamespace != null)
            StringBuilder.AppendLine("}");
    }
}

public class CommentBuilder : StringBuilderGenerationBase
{
    public CommentBuilder(StringBuilder stringBuilder) : base(stringBuilder)
    {
        StringBuilder.AppendLine("/*\n");
    }

    protected override void AppendBottom()
    {
        StringBuilder.AppendLine("*/");
    }
}

//TODO: implement proper solution once I have time for it
public class BracketsBuilder : StringBuilderGenerationBase
{
    private readonly int _indentLevel;

    public BracketsBuilder(StringBuilder stringBuilder, int indentLevel) : base(stringBuilder)
    {
        _indentLevel = indentLevel;
        switch (_indentLevel)
        {
            case 0:
                StringBuilder.AppendLine("{");
                break;
            case 1:
                StringBuilder.AppendLine("    {");
                break;
            default:
                StringBuilder.AppendLine("        {");
                break;
        }
    }

    protected override void AppendBottom()
    {
        switch (_indentLevel)
        {
            case 0:
                StringBuilder.AppendLine("}");
                break;
            case 1:
                StringBuilder.AppendLine("    }");
                break;
            default:
                StringBuilder.AppendLine("        }");
                break;
        }
    }
}
