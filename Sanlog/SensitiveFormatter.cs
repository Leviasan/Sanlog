using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Sanlog
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class SensitiveAttribute : Attribute { }

    /// <summary>
    /// Represents a formatter that supports the concealment of confidential data.
    /// </summary>
    public sealed class SensitiveFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// The format string is used to redact sensitive values.
        /// </summary>
        public const string FormatRedacted = "R";
        /// <summary>
        /// The format string is used to serialize public properties to a string representation.
        /// </summary>
        public const string FormatSerialize = "S";
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
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            const string EmptyObject = "{}";

            if (Equals(formatProvider) && !string.IsNullOrEmpty(format) && arg is not null)
            {
                if (format.Equals(FormatRedacted, StringComparison.Ordinal))
                {
                    return RedactedValue; // TODO: Collapse Type.IsPrimitive array
                }
                else if (format.Equals(FormatSerialize, StringComparison.Ordinal))
                {
                    StringBuilder? stringBuilder = null;
                    var properties = arg.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    for (var index = 0; index < properties.Length; ++index)
                    {
                        var property = properties[index];
                        stringBuilder = stringBuilder is null ? new StringBuilder(256).Append('{') : stringBuilder;
                        _ = stringBuilder
                            .Append(' ')
                            .Append(property.Name)
                            .Append(" = ")
                            .Append(Format(
                                format: property.IsDefined(typeof(SensitiveAttribute)) ? FormatRedacted : null,
                                arg: property.GetValue(arg),
                                formatProvider: this))
                            .Append(index < properties.Length - 1 ? ',' : ' ');
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