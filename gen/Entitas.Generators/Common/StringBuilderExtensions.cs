using System;
using System.Collections.Generic;
using System.Text;

namespace Entitas.Generators.Common;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendJoin(this StringBuilder builder, IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            builder.Append(item);
        }
        return builder;
    }

    public static StringBuilder AppendJoin<T>(this StringBuilder builder, IEnumerable<T> items, Func<T, string> transform)
    {
        foreach (var item in items)
        {
            builder.Append(transform(item));
        }
        return builder;
    }

    public static StringBuilder AppendJoin<T>(this StringBuilder builder, IEnumerable<T> items, Action<StringBuilder, T> transform)
    {
        foreach (var item in items)
        {
            transform(builder, item);
        }
        return builder;
    }

    public static StringBuilder AppendJoin(this StringBuilder builder, char separator, IEnumerable<string> items)
    {
        bool first = true;
        foreach (var item in items)
        {
            if (!first)
                builder.Append(separator);
            else
                first = false;
            builder.Append(item);
        }
        return builder;
    }

    public static StringBuilder AppendJoin<T>(this StringBuilder builder, char separator, IEnumerable<T> items, Func<T, string> transform)
    {
        bool first = true;
        foreach (var item in items)
        {
            if (!first)
                builder.Append(separator);
            else
                first = false;
            builder.Append(transform(item));
        }
        return builder;
    }
    public static StringBuilder AppendJoin<T>(this StringBuilder builder, char separator, IEnumerable<T> items, Action<StringBuilder, T>  transform)
    {
        bool first = true;
        foreach (var item in items)
        {
            if (!first)
                builder.Append(separator);
            else
                first = false;
            transform(builder, item);
        }
        return builder;
    }

    public static StringBuilder AppendJoin(this StringBuilder builder, string separator, IEnumerable<string> items)
    {
        bool first = true;
        foreach (var item in items)
        {
            if (!first)
                builder.Append(separator);
            else
                first = false;
            builder.Append(item);
        }

        return builder;
    }

    public static StringBuilder AppendJoin<T>(this StringBuilder builder, string separator, IEnumerable<T> items, Func<T, string> transform)
    {
        bool first = true;
        foreach (var item in items)
        {
            if (!first)
                builder.Append(separator);
            else
                first = false;
            builder.Append(transform(item));
        }

        return builder;
    }
    public static StringBuilder AppendJoin<T>(this StringBuilder builder, string separator, IEnumerable<T> items, Action<StringBuilder, T>  transform)
    {
        bool first = true;
        foreach (var item in items)
        {
            if (!first)
                builder.Append(separator);
            else
                first = false;
            transform(builder, item);
        }
        return builder;
    }
}
