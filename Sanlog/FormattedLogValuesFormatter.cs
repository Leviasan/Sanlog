using System;
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
    /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified object array that contains zero or more objects to format.
    /// </remarks>
    /// <param name="collection">An object array that contains zero or more objects to format.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="collection"/> is <see langword="null"/>.</exception>
    public sealed class FormattedLogValuesFormatter(IReadOnlyCollection<KeyValuePair<string, object?>> collection) : SensitiveFormatter(collection) // ArgumentNullException
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
        /// The element key that represents a structured logging message.
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
        private readonly string? _format = Convert.ToString(collection.SingleOrDefault(x => x.Key == OriginalFormat).Value, null);
        /// <summary>
        /// The configuration of the formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private FormattedLogValuesFormatterOptions? _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class based on the composite/named format string and an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="format">A composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentException">Passed less than the minimum number of arguments that must be passed to a formatting operation.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        public FormattedLogValuesFormatter(string? format, params object?[] args) : this(ParseCompositeArgs(format, args)) // ArgumentException + ArgumentNullException
            => _format = !string.IsNullOrEmpty(format) ? format : null;

        /// <summary>
        /// Gets or sets the configuration of the formatter.
        /// </summary>
        [AllowNull]
        public FormattedLogValuesFormatterOptions FormattedConfiguration
        {
            get => _configuration ??= new FormattedLogValuesFormatterOptions();
            set => _configuration = value;
        }

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
                if (FormattedConfiguration.CollapsePrimitiveArray)
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
                    var index = IndexOf(segment); // Defines the first occurrence of the key instead of directly using GetObject(string, bool) to prevent InvalidOperationException
                    dictionary[segment] = GetObject(index, true).Value;
                }
                return [.. dictionary.Values];
            }
        }
        #endregion

        /// <summary>
        /// Projects each element processes through the formatter into a string through invoke <see cref="Format(string?, object?, IFormatProvider?)"/>.
        /// </summary>
        /// <returns>An enumerable whose elements result from invoking the transform function <see cref="Format(string?, object?, IFormatProvider?)"/> on each element.</returns>
        public IReadOnlyList<KeyValuePair<string, string?>> Process() => this.Select(x => KeyValuePair.Create(x.Key, (string?)Format(null, x.Value, this))).ToList();
    }
}