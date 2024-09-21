using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    /// <remarks>
    /// Overrides standard behavior of the format string component:
    /// <list type="table">
    ///     <item>
    ///         <term><see cref="DateTime"/></term>
    ///         <description>Uses a round-trip date/time pattern "O" defined in ISO 8601 to format to a string representation.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="DateTimeOffset"/></term>
    ///         <description>Uses a round-trip date/time pattern "O" defined in ISO 8601 to format to a string representation.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="Enum"/></term>
    ///         <description>Uses a decimal pattern "D" to display the enumeration entry as an integer value in the shortest representation possible.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="float"/></term>
    ///         <description>Uses the "G9" format specifier to ensure that the original value successfully round-trips (IEEE 754-2008-compliant).</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="double"/></term>
    ///         <description>Uses the "G17" format specifier to ensure that the original value successfully round-trips (IEEE 754-2008-compliant).</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IEnumerable"/></term>
    ///         <description>Formats value as [object, object2, ..., objectN].</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IEnumerable"/> of <see cref="Type.IsPrimitive"/></term>
    ///         <description>Formats value as "[*{ElementCount} {Type.Name}*]".</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IDictionary"/></term>
    ///         <description>Formats value as [[Key1, Value1], [Key2, Value2]].</description>
    ///     </item>
    /// </list>
    /// </remarks>
    public sealed class FormattedLogValuesFormatter : SensitiveFormatter
    {
        /// <summary>
        /// The message format that represents a null value.
        /// </summary>
        public const string NullValue = "(null)";
        /// <summary>
        /// The message format that represents a null format.
        /// </summary>
        public const string NullFormat = "[null]";
        /// <summary>
        /// The message format that represents a structured logging message.
        /// </summary>
        public const string OriginalFormat = "{OriginalFormat}";
        /// <summary>
        /// The message format that represents a primitive type array.
        /// </summary>
        public static readonly CompositeFormat ArrayFormat = CompositeFormat.Parse("[*{0} {1}*]");

        /// <summary>
        /// Max cached collection size.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private const int MaxCachedTemplates = 1024; // Microsoft.Extensions.Logging.FormattedLogValues.MaxCachedFormatters
        /// <summary>
        /// The cache of the message templates.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<string, MessageTemplate> CachedMessageTemplates = [];

        /// <summary>
        /// Creates a new instance of the <see cref="FormattedLogValuesFormatter"/> class based on a message template, and an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="cultureInfo">An object that supplies culture-specific formatting information.</param>
        /// <param name="configuration">The configuration of the sensitive data.</param>
        /// <param name="format">A composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A new instance of the <see cref="FormattedLogValuesFormatter"/> based on a message template, and an object array that contains zero or more objects to format.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="format"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        public static FormattedLogValuesFormatter Create(CultureInfo? cultureInfo, SensitiveConfiguration? configuration, string format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);

            var dictionary = new Dictionary<string, object?>();
            if (TryGetOrAdd(format, out var messageTemplate)) // FormatException
            {
                for (int index = 0, segmentId = 0; index < args.Length; ++index, ++segmentId)
                {
                    while (segmentId < messageTemplate.Segments.Count && dictionary.Any(x => x.Key == messageTemplate.Segments[segmentId]))
                    {
                        ++segmentId;
                    }
                    dictionary.Add(messageTemplate.Segments[segmentId], args[index]);
                }
                dictionary.Add(OriginalFormat, format);
            }
            else
            {
                dictionary = args.Select((element, index) => KeyValuePair.Create(index.ToString(null, cultureInfo), element)).ToDictionary();
            }
            return new FormattedLogValuesFormatter(dictionary, configuration) { CultureInfo = cultureInfo };
        }
        /// <summary>
        /// Creates a new instance of the <see cref="FormattedLogValuesFormatter"/> class based on a message template, and an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="format">A composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A new instance of the <see cref="FormattedLogValuesFormatter"/> based on a message template, and an object array that contains zero or more objects to format.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="format"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        public static FormattedLogValuesFormatter Create(string format, params object?[] args) => Create(null, null, format, args);
        /// <summary>
        /// Tries to get a message template from the cache or parses and tries to add to cache one.
        /// </summary>
        /// <param name="format">The original message template value.</param>
        /// <param name="messageTemplate">When this method returns, it contains a message template or the <see langword="null"/> if the operation failed.</param>
        /// <returns><see langword="true"/> if the operation is successful; otherwise <see langword="false"/>.</returns>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        private static bool TryGetOrAdd(string? format, [NotNullWhen(true)] out MessageTemplate? messageTemplate)
        {
            if (!string.IsNullOrEmpty(format))
            {
                if (!CachedMessageTemplates.TryGetValue(format, out messageTemplate))
                {
                    messageTemplate = new MessageTemplate(format); // FormatException
                    return messageTemplate.CompositeFormat.MinimumArgumentCount <= 0 || CachedMessageTemplates.Count >= MaxCachedTemplates || CachedMessageTemplates.TryAdd(format, messageTemplate);
                }
                return true;
            }
            messageTemplate = default;
            return false;
        }

        /// <summary>
        /// The message template.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MessageTemplate? _messageTemplate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified raw values.
        /// </summary>
        /// <param name="dictionary">The raw values.</param>
        /// <param name="configuration">The configuration of the sensitive data.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="dictionary"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        public FormattedLogValuesFormatter(IReadOnlyDictionary<string, object?> dictionary, SensitiveConfiguration? configuration) : base(dictionary, configuration)
        {
            _messageTemplate = TryGetOrAdd( // FormatException
                format: dictionary.TryGetValue(OriginalFormat, out var value) ? value as string : null,
                messageTemplate: out var messageTemplate) ? messageTemplate : null;
        }

        /// <summary>
        /// Gets or sets a value indicating whether a primitive type array will be formatted.
        /// </summary>
        public bool PrimitiveTypeArray { get; set; }

        #region Override: SensitiveFormatter
        /// <inheritdoc/>
        public override string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            const string EmptyArray = "[]";
            if (Equals(formatProvider) && string.IsNullOrEmpty(format) && TryCustomFormat(arg, formatProvider, base.Format, out var stringValue))
            {
                return stringValue;
            }
            return base.Format(format, arg, formatProvider);

            bool TryCustomFormat(object? value, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = value switch
                {
                    DateTime dateTime => dateTime.ToString("O", formatProvider),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", formatProvider),
                    Enum @enum => @enum.ToString("D"),
                    float binary32 => binary32.ToString("G9", formatProvider),
                    double binary64 => binary64.ToString("G17", formatProvider),
                    string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => IDictionaryToString(dictionary, formatProvider, formatter), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => IEnumerableToString(enumerable, formatProvider, formatter),
                    null => NullValue,
                    _ => null
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            string ObjectToString(object? value, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter)
            {
                return TryCustomFormat(value, formatProvider, formatter, out var stringValue) ? stringValue : formatter.Invoke(null, value, formatProvider);
            }
            string IEnumerableToString(IEnumerable enumerable, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter)
            {
                if (PrimitiveTypeArray)
                {
                    var type = enumerable.GetType();
                    if (type.IsArray)
                    {
                        var elementType = type.GetElementType();
                        if (elementType is not null && elementType.IsPrimitive)
                            return ArrayToFormat(enumerable, elementType, formatProvider);
                    }
                    else if (type.IsGenericType && type.GenericTypeArguments.Length == 1)
                    {
                        var elementType = type.GenericTypeArguments.First();
                        if (elementType.IsPrimitive)
                            return ArrayToFormat(enumerable, elementType, formatProvider);
                    }
                }

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
            static string ArrayToFormat(IEnumerable enumerable, Type type, IFormatProvider? formatProvider)
            {
                return string.Format(formatProvider, ArrayFormat, IEnumerableCount(enumerable), type.Name);

                static int IEnumerableCount(IEnumerable enumerable)
                {
                    int count = 0;
                    foreach (var item in enumerable)
                        ++count;
                    return count;
                }
            }
        }
        #endregion

        /// <summary>
        /// Gets a key-value pair describing a property name and string representation of the object.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The key-value pair describing a property name and string representation of the object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> GetObjectAsString(int index, bool redacted)
        {
            var pair = GetObject(index, redacted); // ArgumentOutOfRangeException
            var stringRepresentation = Format(null, pair.Value, this);
            return KeyValuePair.Create(pair.Key, stringRepresentation);
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and string representation of the object.
        /// </summary>
        /// <param name="name">The property name to find.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The key-value pair describing a property name and string representation of the object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="name"/> is not found.</exception>
        public KeyValuePair<string, string> GetObjectAsString(string name, bool redacted)
        {
            var pair = GetObject(name, redacted); // ArgumentNullException + KeyNotFoundException
            var stringRepresentation = Format(null, pair.Value, this);
            return KeyValuePair.Create(pair.Key, stringRepresentation);
        }
        /// <summary>
        /// Replaces format items in the composite string and builds result string.
        /// </summary>
        /// <returns>The string representation of the values formatted by the formatter.</returns>
        public string ToMessage()
        {
            return _messageTemplate is not null ? _messageTemplate.Format(this, TakeBySegmentOrder(_messageTemplate)) : NullFormat;

            object?[] TakeBySegmentOrder(MessageTemplate messageTemplate)
            {
                var dictionary = new Dictionary<string, object?>();
                foreach (var segment in messageTemplate.Segments)
                {
                    if (dictionary.ContainsKey(segment)) continue;
                    dictionary[segment] = GetObject(segment, true).Value;
                }
                return [.. dictionary.Values];
            }
        }
    }
}