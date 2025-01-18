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
    public sealed class FormattedLogValuesFormatter : CustomFormatter
    {
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
            const string NullValue = "(null)";

            return Equals(formatProvider) && string.IsNullOrEmpty(format) && TryCustomFormat(arg, formatProvider, base.Format, out var stringValue) ? stringValue : base.Format(format, arg, formatProvider);

            bool TryCustomFormat(object? value, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = value switch
                {
                    DateTime dateTime => dateTime.ToString(Configuration.DateTimeFormat, formatProvider),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(Configuration.DateTimeOffsetFormat, formatProvider),
                    Enum @enum => @enum.ToString(Configuration.EnumFormat),
                    float binary32 => binary32.ToString(Configuration.SingleFormat, formatProvider),
                    double binary64 => binary64.ToString(Configuration.DoubleFormat, formatProvider),
                    string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => IDictionaryToString(dictionary, formatProvider, formatter), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => IEnumerableToString(enumerable, formatProvider, formatter),
                    null => NullValue,
                    _ => null
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            string ObjectToString(object? value, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter)
                => TryCustomFormat(value, formatProvider, formatter, out var stringValue) ? stringValue : formatter.Invoke(null, value, formatProvider);
            string IEnumerableToString(IEnumerable enumerable, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter)
            {
                var first = true;
                StringBuilder? stringBuilder = null;
                foreach (var value in enumerable)
                {
                    stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                    stringBuilder = stringBuilder.Append(ObjectToString(value, formatProvider, formatter));
                    first = false;
                }
                return stringBuilder?.Append(']').ToString() ?? EmptyArray;
            }
            string IDictionaryToString(IDictionary dictionary, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter)
            {
                var first = true;
                StringBuilder? stringBuilder = null;
                foreach (DictionaryEntry entry in dictionary)
                {
                    stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                    stringBuilder = stringBuilder.Append(formatProvider, $"[{ObjectToString(entry.Key, formatProvider, formatter)}, {ObjectToString(entry.Value, formatProvider, formatter)}]");
                    first = false;
                }
                return stringBuilder?.Append(']').ToString() ?? EmptyArray;
            }
        }
    }
}