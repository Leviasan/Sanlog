﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Sanlog
{
    /// <summary>
    /// Represents a formatter that supports the concealment of confidential data.
    /// </summary>
    public sealed class SensitiveFormatter : CustomFormatter
    {
        /// <summary>
        /// The configuration of the formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SensitiveFormatterOptions? _configuration;

        /// <summary>
        /// Gets or sets the configuration of the formatter.
        /// </summary>
        [AllowNull]
        public SensitiveFormatterOptions Configuration
        {
            get => _configuration ??= new SensitiveFormatterOptions();
            set => _configuration = value;
        }

        /// <inheritdoc/>
        public override string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            const string FormatRedacted = "R";
            const string FormatProperty = "P";
            const string FormatCollapse = "C";
            const string RedactedValue = "[Redacted]";
            const string FormatCollapseArray = "[*{0} {1}*]";

            if (Equals(formatProvider) && format is not null && arg is not null)
            {
                if (format.Equals(FormatRedacted, StringComparison.Ordinal))
                {
                    return RedactedValue;
                }
                else if (format.Equals(FormatProperty, StringComparison.Ordinal))
                {
                    var props = arg.GetType().GetProperties();
                    var nodes = props.Select(x => KeyValuePair.Create(x.Name, x.GetValue(arg))).ToArray();
                    var stringBuilder = new StringBuilder(256).Append('{');
                    for (var index = 0; index < nodes.Length; ++index)
                    {
                        var node = nodes[index];
                        _ = stringBuilder.Append(' ').Append(node.Key).Append(" = ").Append(Format(null, node.Value, this));
                        _ = index < nodes.Length - 1 ? stringBuilder.Append(',') : stringBuilder.Append(' ');
                    }
                    return stringBuilder.Append('}').ToString();
                }
                else if (format.Equals(FormatCollapse, StringComparison.Ordinal) && arg is IEnumerable enumerable)
                {
                    var type = enumerable.GetType();
                    if (type.IsArray)
                    {
                        var elementType = type.GetElementType();
                        if (elementType is not null)
                            return ArrayToFormat(enumerable, elementType, this);
                    }
                    else if (type.IsGenericType && type.GenericTypeArguments.Length == 1)
                    {
                        var elementType = type.GenericTypeArguments[0];
                        return ArrayToFormat(enumerable, elementType, this);
                    }
                }
            }
            return base.Format(format, arg, formatProvider);

            static string ArrayToFormat(IEnumerable enumerable, Type type, IFormatProvider? formatProvider)
            {
                return string.Format(formatProvider, FormatCollapseArray, IEnumerableCount(enumerable), type.Name);

                static int IEnumerableCount(IEnumerable enumerable)
                {
                    var count = 0;
                    foreach (var item in enumerable)
                        ++count;
                    return count;
                }
            }
        }
    }
}