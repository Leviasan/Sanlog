using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Sanlog
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    public sealed class FormattedLogValuesFormatter : IFormatProvider, ICustomFormatter
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
        /// The message format that represents a null value.
        /// </summary>
        public const string NullValue = "(null)";
        /// <summary>
        /// The message format that represents a redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";

        /// <summary>
        /// The configuration of the formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private FormattedLogValuesFormatterOptions? _configuration;

        /// <summary>
        /// Gets or sets the configuration of the formatter.
        /// </summary>
        [AllowNull]
        public FormattedLogValuesFormatterOptions Configuration
        {
            get => _configuration ??= new FormattedLogValuesFormatterOptions
            {
                DateTimeFormat = "O",
                DateTimeOffsetFormat = "O",
                EnumFormat = "D",
                SingleFormat = "G9",
                DoubleFormat = "G17"
            };
            set => _configuration = value;
        }
        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }

        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : CultureInfo?.GetFormat(formatType);

        /// <inheritdoc/>
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            Configuration.MakeReadOnly(); // Disallows to change configuration while formatting

            if (Equals(formatProvider))
            {
                if (string.IsNullOrEmpty(format))
                {
                    if (OverrideToString(arg, formatProvider, Configuration) is string stringValue && !string.IsNullOrEmpty(stringValue))
                    {
                        return stringValue;
                    }
                }
                else
                {
                    if (format.Equals(FormatRedacted, StringComparison.Ordinal))
                    {
                        return RedactedValue;
                    }
                    if (format.Equals(FormatSerialize, StringComparison.Ordinal) && arg is not null)
                    {
                        var stringValue = arg switch
                        {
                            string str => str, // string implements IEnumerable so must be process before
                            IDictionary dictionary => IDictionaryToString(dictionary, formatProvider, formatter, configuration), // IDictionary implements IEnumerable so must be process before
                            IEnumerable enumerable => IEnumerableToString(enumerable, formatProvider, formatter, configuration),
                            _ => SerializeObject(arg, formatProvider)
                        };

                        /*
                        var type = arg.GetType();
                        var stringValue = arg switch
                        {
                            string str => str, // string implements IEnumerable so must be process before
                            IDictionary dictionary => IDictionaryToString(dictionary, formatProvider, formatter, configuration), // IDictionary implements IEnumerable so must be process before
                            IEnumerable enumerable => IEnumerableToString(enumerable, formatProvider, formatter, configuration),
                            _ => throw new NotImplementedException()
                        };
                        */

                        return Serialize(arg, arg.GetType(), this);
                    }
                }
            }
            return DefaultFallback(format, arg, Equals(formatProvider) ? CultureInfo : formatProvider);

            static string DefaultFallback(string? format, object? arg, IFormatProvider? formatProvider)
            {
                return arg switch
                {
                    IFormattable formattable => formattable.ToString(format, formatProvider),
                    _ => Convert.ToString(arg, formatProvider) ?? string.Empty
                };
            };
            static string? OverrideToString(object? arg, IFormatProvider? formatProvider, FormattedLogValuesFormatterOptions configuration)
            {
                return arg switch
                {
                    DateTime dateTime => dateTime.ToString(configuration.DateTimeFormat, formatProvider),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(configuration.DateTimeOffsetFormat, formatProvider),
                    Enum @enum => @enum.ToString(configuration.EnumFormat),
                    float binary32 => binary32.ToString(configuration.SingleFormat, formatProvider),
                    double binary64 => binary64.ToString(configuration.DoubleFormat, formatProvider),
                    null => NullValue,
                    _ => null
                };
            }
            static string SerializeObject(object value, IFormatProvider? provider)
            {
                const string EmptyObject = "{}";
                const string SimpleFormat = "{0}";
                const string RedactedFormat = "{0:R}";

                StringBuilder? stringBuilder = null;
                var type = value.GetType();
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                for (var index = 0; index < properties.Length; ++index)
                {
                    var property = properties[index];
                    stringBuilder = stringBuilder is null ? new StringBuilder(256).Append('{') : stringBuilder;
                    _ = stringBuilder
                        .Append(' ')
                        .Append(property.Name)
                        .Append(" = ")
                        .Append(string.Format(provider, property.IsDefined(typeof(SensitiveAttribute)) ? RedactedFormat : SimpleFormat, property.GetValue(value)))
                        .Append(index < properties.Length - 1 ? ',' : ' ');
                }
                return stringBuilder?.Append('}').ToString() ?? EmptyObject;
            }
            static string IDictionarySerialize(IDictionary dictionary, IFormatProvider formatProvider)
            {
                const string EmptyArray = "[]";

                var first = true;
                StringBuilder? stringBuilder = null;
                foreach (DictionaryEntry entry in dictionary)
                {
                    stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                    stringBuilder = stringBuilder.Append(formatProvider, $"[{Serialize(entry.Key, entry.Key.GetType(), formatProvider)}, {ObjectToString(entry.Value, formatProvider, formatter, configuration)}]");
                    first = false;
                }
                return stringBuilder?.Append(']').ToString() ?? EmptyArray;
            }
            // ----------------------------------------- NULLABLE FORMAT -----------------------------------------
            /*
             *                     string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => IDictionarySerialize(dictionary, formatProvider, formatter, configuration), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => IEnumerableToString(enumerable, formatProvider, formatter, configuration),

            static bool TryCustomFormat(object? value, IFormatProvider? formatProvider, FormattedLogValuesFormatterOptions configuration, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = value switch
                {
                    DateTime dateTime => dateTime.ToString(configuration.DateTimeFormat, formatProvider),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(configuration.DateTimeOffsetFormat, formatProvider),
                    Enum @enum => @enum.ToString(configuration.EnumFormat),
                    float binary32 => binary32.ToString(configuration.SingleFormat, formatProvider),
                    double binary64 => binary64.ToString(configuration.DoubleFormat, formatProvider),
                  
                    null => NullValue,
                    _ => null
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            */
            /*

            static string IEnumerableToString(IEnumerable enumerable, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter, FormattedLogValuesFormatterOptions configuration)
            {
                var first = true;
                StringBuilder? stringBuilder = null;
                foreach (var value in enumerable)
                {
                    stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                    stringBuilder = stringBuilder.Append(ObjectToString(value, formatProvider, formatter, configuration));
                    first = false;
                }
                return stringBuilder?.Append(']').ToString() ?? EmptyArray;
            }
            static string ObjectToString(object? value, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter, FormattedLogValuesFormatterOptions configuration)
                => TryCustomFormat(value, formatProvider, formatter, configuration, out var stringValue) ? stringValue : formatter.Invoke(null, value, formatProvider);
            */
            // ----------------------------------------- NULLABLE FORMAT -----------------------------------------
        }
    }
}