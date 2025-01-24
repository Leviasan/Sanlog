using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Sanlog
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    public sealed class FormattedLogValuesFormatter : SensitiveFormatter
    {
        /// <summary>
        /// The message format that represents a null value.
        /// </summary>
        public const string NullValue = "(null)";

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

        /// <inheritdoc/>
        public override string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            const string EmptyArray = "[]";
            return Equals(formatProvider) && string.IsNullOrEmpty(format) && TryCustomFormat(arg, formatProvider, base.Format, Configuration, out var stringValue) ? stringValue : base.Format(format, arg, formatProvider);

            static bool TryCustomFormat(object? value, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter, FormattedLogValuesFormatterOptions configuration, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = value switch
                {
                    DateTime dateTime => dateTime.ToString(configuration.DateTimeFormat, formatProvider),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(configuration.DateTimeOffsetFormat, formatProvider),
                    Enum @enum => @enum.ToString(configuration.EnumFormat),
                    float binary32 => binary32.ToString(configuration.SingleFormat, formatProvider),
                    double binary64 => binary64.ToString(configuration.DoubleFormat, formatProvider),
                    string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => IDictionaryToString(dictionary, formatProvider, formatter, configuration), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => IEnumerableToString(enumerable, formatProvider, formatter, configuration),
                    null => NullValue,
                    _ => null
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            static string IDictionaryToString(IDictionary dictionary, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter, FormattedLogValuesFormatterOptions configuration)
            {
                var first = true;
                StringBuilder? stringBuilder = null;
                foreach (DictionaryEntry entry in dictionary)
                {
                    stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                    stringBuilder = stringBuilder.Append(formatProvider, $"[{ObjectToString(entry.Key, formatProvider, formatter, configuration)}, {ObjectToString(entry.Value, formatProvider, formatter, configuration)}]");
                    first = false;
                }
                return stringBuilder?.Append(']').ToString() ?? EmptyArray;
            }
            static string ObjectToString(object? value, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter, FormattedLogValuesFormatterOptions configuration)
                => TryCustomFormat(value, formatProvider, formatter, configuration, out var stringValue) ? stringValue : formatter.Invoke(null, value, formatProvider);
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
        }
    }
}