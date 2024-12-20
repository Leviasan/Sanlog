﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Sanlog
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
    ///         <description>Formats value as [object, object2, ..., objectN] or [*{ElementCount} {Type.Name}*] if <see cref="Type.IsPrimitive"/> and <see cref="FormatPrimitiveArray"/> is <see langword="true"/>.</description>
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
        public static readonly CompositeFormat PrimitiveArrayFormat = CompositeFormat.Parse("[*{0} {1}*]");

        /// <summary>
        /// Max cached collection size.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private const int MaxCachedTemplates = 1024; // Microsoft.Extensions.Logging.FormattedLogValues.MaxCachedFormatters
        /// <summary>
        /// The cache of the message templates.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly ConcurrentDictionary<string, MessageTemplate> CachedMessageTemplates = [];

        /// <summary>
        /// Tries to get a message template from the cache or parses composite/named format string and tries to add to cache one.
        /// </summary>
        /// <param name="format">A composite/named format string.</param>
        /// <param name="messageTemplate">When this method returns, it contains a message template or the <see langword="null"/> if the operation failed.</param>
        /// <returns><see langword="true"/> if the operation is successful; otherwise <see langword="false"/>.</returns>
        /// <exception cref="FormatException">A format item in <paramref name="format"/> is invalid.</exception>
        private static bool TryGetOrAdd(string? format, [NotNullWhen(true)] out MessageTemplate? messageTemplate)
        {
            if (!string.IsNullOrEmpty(format))
            {
                if (!CachedMessageTemplates.TryGetValue(format, out messageTemplate))
                {
                    messageTemplate = new MessageTemplate(format); // FormatException
                    return messageTemplate.CompositeFormat.MinimumArgumentCount == 0 || CachedMessageTemplates.Count >= MaxCachedTemplates || CachedMessageTemplates.TryAdd(format, messageTemplate) || true;
                }
                return true;
            }
            messageTemplate = default;
            return false;
        }
        /// <summary>
        /// Creates a dictionary from an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="format">A composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A dictionary from an object array that contains zero or more objects to format.</returns>
        /// <exception cref="ArgumentException">Passed less than the minimum number of arguments that must be passed to a formatting operation.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        private static Dictionary<string, object?> ParseCompositeArgs(string? format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            var index = 0;
            var dictionary = new Dictionary<string, object?>();
            if (TryGetOrAdd(format, out var messageTemplate)) // FormatException
            {
                if (args.Length < messageTemplate.CompositeFormat.MinimumArgumentCount)
                {
                    throw new ArgumentException("Passed less than the minimum number of arguments that must be passed to a formatting operation.", nameof(args));
                }
                foreach (var segment in messageTemplate)
                {
                    if (dictionary.ContainsKey(segment))
                    {
                        continue;
                    }
                    dictionary.Add(segment, args[index++]);
                }
            }
            if (index < args.Length)
            {
                var extendedParams = args.Select((element, index) => KeyValuePair.Create($"args[{index}]", element)).Skip(index);
                dictionary = dictionary.Concat(extendedParams).ToDictionary();
            }
            if (!string.IsNullOrEmpty(format))
            {
                dictionary.Add(OriginalFormat, format);
            }
            return dictionary;
        }

        /// <summary>
        /// The composite/named format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _format;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class based on the composite/named format string and an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="format">A composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentException">Passed less than the minimum number of arguments that must be passed to a formatting operation.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        public FormattedLogValuesFormatter(string? format, params object?[] args) : base(ParseCompositeArgs(format, args)) // ArgumentException + ArgumentNullException
            => _format = !string.IsNullOrEmpty(format) ? format : null;
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="dictionary">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="dictionary"/> is <see langword="null"/>.</exception>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Base class validate param to null")]
        public FormattedLogValuesFormatter(IReadOnlyDictionary<string, object?> dictionary) : base(dictionary) // ArgumentNullException
            => _format = dictionary.TryGetValue(OriginalFormat, out var value) ? Convert.ToString(value, null) : null;
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="enumerable">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="enumerable"/> is <see langword="null"/>.</exception>
        public FormattedLogValuesFormatter(IEnumerable<object?> enumerable) : base(enumerable) { } // ArgumentNullException

        /// <summary>
        /// Gets or sets a value indicating whether a primitive type array will be formatted.
        /// </summary>
        public bool FormatPrimitiveArray { get; set; }

        #region Override: SensitiveFormatter
        /// <inheritdoc/>
        public override string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            const string EmptyArray = "[]";
            return Equals(formatProvider) && string.IsNullOrEmpty(format) && TryCustomFormat(arg, formatProvider, base.Format, out var stringValue)
                ? stringValue
                : base.Format(format, arg, formatProvider);

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
                => TryCustomFormat(value, formatProvider, formatter, out var stringValue) ? stringValue : formatter.Invoke(null, value, formatProvider);
            string IEnumerableToString(IEnumerable enumerable, IFormatProvider? formatProvider, Func<string?, object?, IFormatProvider?, string> formatter)
            {
                if (FormatPrimitiveArray)
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
                return string.Format(formatProvider, PrimitiveArrayFormat, IEnumerableCount(enumerable), type.Name);

                static int IEnumerableCount(IEnumerable enumerable)
                {
                    var count = 0;
                    foreach (var item in enumerable)
                        ++count;
                    return count;
                }
            }
        }
        #endregion

        #region Override: Object
        /// <inheritdoc/>
        public override string ToString()
        {
            return TryGetOrAdd(_format, out var messageTemplate) ? messageTemplate.Format(this, TakeBySegmentOrder(messageTemplate)) : NullFormat;

            object?[] TakeBySegmentOrder(MessageTemplate messageTemplate)
            {
                var dictionary = new Dictionary<string, object?>();
                foreach (var segment in messageTemplate)
                {
                    if (dictionary.ContainsKey(segment))
                    {
                        continue;
                    }
                    dictionary[segment] = GetObject(segment, true).Value;
                }
                return [.. dictionary.Values];
            }
        }
        #endregion

        /// <summary>
        /// Projects each element processes through the formatter into a string through invoke <see cref="Format(string?, object?, IFormatProvider?)"/>.
        /// </summary>
        /// <returns>A dictionary whose elements result from invoking the transform function <see cref="Format(string?, object?, IFormatProvider?)"/> on each element.</returns>
        public Dictionary<string, string?> ToStringDictionary() => this.ToDictionary(ks => ks.Key, es => (string?)Format(null, es.Value, this));
    }
}