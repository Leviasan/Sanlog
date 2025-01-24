using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sanlog
{
    /// <summary>
    /// Represents a formatter that supports the concealment of confidential data.
    /// </summary>
    public class SensitiveFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// The message format that represents a redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";

        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }

        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : CultureInfo?.GetFormat(formatType);

        /// <inheritdoc/>
        public virtual string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            const string EmptyObject = "{}";
            if (Equals(formatProvider) && !string.IsNullOrEmpty(format) && arg is not null)
            {
                if (format.Equals("R", StringComparison.Ordinal))
                {
                    return RedactedValue;
                }
                else if (format.Equals("S", StringComparison.Ordinal))
                {
                    var props = arg.GetType().GetProperties();
                    var nodes = props.Select(x => KeyValuePair.Create(x.Name, x.GetValue(arg))).ToArray();
                    StringBuilder? stringBuilder = null;
                    for (var index = 0; index < nodes.Length; ++index)
                    {
                        var node = nodes[index];
                        stringBuilder = (index == 0 ? new StringBuilder(256).Append('{') : stringBuilder!)
                            .Append(' ')
                            .Append(node.Key)
                            .Append(" = ")
                            .Append(Format(null, node.Value, this))
                            .Append(index < nodes.Length - 1 ? ',' : ' ');
                    }
                    return stringBuilder?.Append('}').ToString() ?? EmptyObject;
                }
                else if (format.Equals("C", StringComparison.Ordinal) && arg is IEnumerable enumerable)
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
            var provider = Equals(formatProvider) ? CultureInfo : formatProvider;
            return arg switch
            {
                IFormattable formattable => formattable.ToString(format, provider),
                _ => Convert.ToString(arg, provider) ?? string.Empty
            };

            static string ArrayToFormat(IEnumerable enumerable, Type type, IFormatProvider? formatProvider)
            {
                return string.Format(formatProvider, "[*{0} {1}*]", IEnumerableCount(enumerable), type.Name);

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