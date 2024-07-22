using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    ///         <term><see cref="IDictionary"/></term>
    ///         <description>Formats value as [[Key1, Value1], [Key2, Value2]].</description>
    ///     </item>
    /// </list>
    /// </remarks>
    public sealed class FormattedLogValuesFormatter : IFormatProvider, ICustomFormatter, IReadOnlyList<KeyValuePair<string, string>>
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
        /// The message format that represents a redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";
        /// <summary>
        /// The message format that represents a structured logging message.
        /// </summary>
        public const string OriginalFormat = "{OriginalFormat}";

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
                    messageTemplate = MessageTemplate.Parse(format, null); // FormatException
                    return messageTemplate.MinimumArgumentCount <= 0 || CachedMessageTemplates.Count >= MaxCachedTemplates || CachedMessageTemplates.TryAdd(format, messageTemplate);
                }
                return true;
            }
            messageTemplate = default;
            return false;
        }

        /// <summary>
        /// The dictionary of raw values.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<KeyValuePair<string, object?>> _dictionary;
        /// <summary>
        /// An object that supplies culture-specific formatting information.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IFormatProvider? _formatProvider;
        /// <summary>
        /// The message template.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MessageTemplate? _messageTemplate;
        /// <summary>
        /// The dictionary of the sensitive data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, HashSet<string>> _sensitiveDataType = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified dictionary of raw values and an object that supplies culture-specific formatting information.
        /// </summary>
        /// <param name="dictionary">A dictionary of raw values.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        /// <exception cref="InvalidOperationException">More than one <see cref="OriginalFormat"/> key was found at <paramref name="dictionary"/> array.</exception>
        public FormattedLogValuesFormatter(IReadOnlyList<KeyValuePair<string, object?>>? dictionary, IFormatProvider? formatProvider)
        {
            _dictionary = dictionary ?? [];
            _formatProvider = formatProvider;
            _messageTemplate = TryGetOrAdd( // FormatException
                format: _dictionary.SingleOrDefault(x => x.Key == OriginalFormat).Value as string,  // InvalidOperationException
                messageTemplate: out var messageTemplate) ? messageTemplate : null;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified object that supplies culture-specific formatting information, a message template, and an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">A message template.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="format"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        public FormattedLogValuesFormatter(IFormatProvider? formatProvider, string format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);

            if (TryGetOrAdd(format, out _messageTemplate)) // FormatException
            {
                var original = new List<KeyValuePair<string, object?>>();
                for (int index = 0, segmentId = 0; index < args.Length; ++index, ++segmentId)
                {
                    while (segmentId < _messageTemplate.Count && original.Any(x => x.Key == _messageTemplate[segmentId].Name))
                    {
                        ++segmentId;
                    }
                    original.Add(KeyValuePair.Create(_messageTemplate[segmentId].Name, args[index]));
                }
                original.Add(KeyValuePair.Create<string, object?>(OriginalFormat, format));
                _dictionary = original;
            }
            else
            {
                _dictionary = args.Select((element, index) => KeyValuePair.Create(index.ToString(null, _formatProvider), element)).ToList();
            }
            _formatProvider = formatProvider;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> this[int index] => GetObjectAsString(index, true);
        /// <summary>
        /// Gets the element at the specified name in the read-only list.
        /// </summary>
        /// <param name="name">The property name of the element to get.</param>
        /// <returns>The element at the specified name in the read-only list.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="name"/> is not found.</exception>
        public KeyValuePair<string, string> this[string name] => GetObjectAsString(name, true);
        /// <inheritdoc/>
        public int Count => _dictionary.Count;
        /// <summary>
        /// Indicates whether <see cref="OriginalFormat"/> key is registered.
        /// </summary>
        public bool HasOriginalFormat => _dictionary.Any(x => x.Key.Equals(OriginalFormat, StringComparison.Ordinal));

        /// <inheritdoc/>
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            const string EmptyArray = "[]";
            return Equals(formatProvider) && string.IsNullOrEmpty(format) && TryCustomFormat(arg, formatProvider, out var stringValue) ? stringValue : arg switch
            {
                IFormattable formattable => formattable.ToString(format, formatProvider),
                _ => FormatFallback(arg, formatProvider)
            };

            // Summary: Tries to convert the value of a specified object to an equivalent string representation using overridden string format and culture-specific formatting information.
            // Param (value): An object to format.
            // Param (formatProvider): An object that supplies format information about the current instance.
            // Param (stringValue): When this method returns, it contains a string representation of the specified value or null if the operation failed.
            // Returns: true if the operation is successful; otherwise false.
            static bool TryCustomFormat(object? value, IFormatProvider? formatProvider, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = value switch
                {
                    DateTime dateTime => dateTime.ToString("O", formatProvider),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", formatProvider),
                    Enum @enum => @enum.ToString("D"),
                    float binary32 => binary32.ToString("G9", formatProvider),
                    double binary64 => binary64.ToString("G17", formatProvider),
                    _ => null
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            // Summary: Converts the value of a specified object to a string representation using custom format and culture-specific formatting information.
            // Param (value): An object to format.
            // Param (formatProvider): An object that supplies format information about the current instance.
            // Returns: A string representation of the specified value.
            static string FormatFallback(object? value, IFormatProvider? formatProvider)
            {
                return value switch
                {
                    // IDictionary implements IEnumerable so must be process before
                    IDictionary dictionary => IDictionaryToString(dictionary, formatProvider),
                    string @string => @string, // string implements IEnumerable so must be process before
                    IEnumerable enumerable => IEnumerableToString(enumerable, formatProvider),
                    _ => null
                } ?? Convert.ToString(value ?? NullValue, formatProvider) ?? string.Empty;

                // Summary: Converts the value of a specified object to a string representation using overridden and custom formats and culture-specific formatting information.
                // Param (value): An object to format.
                // Param (formatProvider): An object that supplies format information about the current instance.
                // Returns: A string representation of the specified value.
                static string ObjectToString(object? value, IFormatProvider? formatProvider) => TryCustomFormat(value, formatProvider, out var stringValue) ? stringValue : FormatFallback(value, formatProvider);
                // Summary: Converts IDictionary object to a string representation using overridden and custom formats and culture-specific formatting information.
                // Param (dictionary): An object to format.
                // Param (formatProvider): An object that supplies format information about the current instance.
                // Returns: A string representation of the specified value.
                static string IDictionaryToString(IDictionary dictionary, IFormatProvider? formatProvider)
                {
                    var first = true;
                    StringBuilder? stringBuilder = null;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                        stringBuilder = stringBuilder.Append(formatProvider, $"[{ObjectToString(entry.Key, formatProvider)}, {ObjectToString(entry.Value, formatProvider)}]");
                        first = false;
                    }
                    return stringBuilder?.Append(']').ToString() ?? EmptyArray;
                }
                // Summary: Converts IEnumerable object to a string representation using overridden and custom formats and culture-specific formatting information.
                // Param (enumerable): An object to format.
                // Param (formatProvider): An object that supplies format information about the current instance.
                // Returns: A string representation of the specified value.
                static string IEnumerableToString(IEnumerable enumerable, IFormatProvider? formatProvider)
                {
                    var first = true;
                    StringBuilder? stringBuilder = null;
                    foreach (var value in enumerable)
                    {
                        stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                        stringBuilder = stringBuilder.Append(ObjectToString(value, formatProvider));
                        first = false;
                    }
                    return stringBuilder?.Append(']').ToString() ?? EmptyArray;
                }
            }
        }
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            for (var index = 0; index < Count; ++index)
                yield return GetObjectAsString(index, true);
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : _formatProvider?.GetFormat(formatType);
        /// <summary>
        /// Gets a key-value pair describing a property name and object considering hiding sensitive data.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to hide sensitive data.</param>
        /// <returns>The key-value pair describing a property name and object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var key = _dictionary[index].Key;
            var value = ProcessSensitiveObject(key, _dictionary[index].Value, redacted);
            return KeyValuePair.Create(key, value);

            // Summary: Hiding sensitive data of a specified object.
            // Param (key): The key of the object.
            // Param (value): The object to format.
            // Param (redacted): Indicates whether need to hide sensitive data.
            // Returns: An object considering hiding sensitive data.
            object? ProcessSensitiveObject(string key, object? value, bool redacted)
            {
                return redacted && IsSensitiveData(typeof(string), key) ? RedactedValue : SensitiveObject(value, redacted, this);

                // Summary: Hiding sensitive data of a specified object.
                // Param (value): The object to format.
                // Param (redacted): Indicates whether need to hide sensitive data.
                // Param (formatter): The current instance of the formatter.
                // Returns: An object considering hiding sensitive data.
                static object? SensitiveObject(object? value, bool redacted, FormattedLogValuesFormatter formatter)
                {
                    return value switch
                    {
                        // IDictionary implements IEnumerable so must be process before
                        IDictionary dictionary => SensitiveDictionary(dictionary, redacted, formatter),
                        string @string => @string, // string implements IEnumerable so must be process before
                        IEnumerable enumerable => SensitiveEnumerable(enumerable, redacted, formatter),
                        _ => value
                    };

                    // Summary: Hiding sensitive data of a specified IDictionary object.
                    // Param (dictionary): An object to format.
                    // Param (redacted): Indicates whether need to hide sensitive data.
                    // Param (formatter): The current instance of the formatter.
                    // Returns: An IDictionary object considering hiding sensitive data.
                    static IDictionary SensitiveDictionary(IDictionary dictionary, bool redacted, FormattedLogValuesFormatter formatter)
                    {
                        var newdict = new Dictionary<object, object?>(dictionary.Count);
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            var key = formatter.Format(null, entry.Key, formatter);
                            var newvalue = redacted && formatter.IsSensitiveData(typeof(DictionaryEntry), key) ? RedactedValue : entry.Value;
                            newdict.Add(entry.Key, newvalue);
                        }
                        return newdict;
                    }
                    // Summary: Hiding sensitive data of a specified IEnumerable object.
                    // Param (dictionary): An object to format.
                    // Param (redacted): Indicates whether need to hide sensitive data.
                    // Param (formatter): The current instance of the formatter.
                    // Returns: An IEnumerable object considering hiding sensitive data.
                    static IEnumerable SensitiveEnumerable(IEnumerable enumerable, bool redacted, FormattedLogValuesFormatter formatter)
                    {
                        var newlist = new List<object?>();
                        foreach (var entry in enumerable)
                            newlist.Add(SensitiveObject(entry, redacted, formatter));
                        return newlist;
                    }
                }
            }
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and object considering hiding sensitive data.
        /// </summary>
        /// <param name="name">The property name to find.</param>
        /// <param name="redacted">Indicates whether need to hide sensitive data.</param>
        /// <returns>The key-value pair describing a property name and object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="name"/> is not found.</exception>
        public KeyValuePair<string, object?> GetObject(string name, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(name);
            for (var index = 0; index < _dictionary.Count; ++index)
                if (_dictionary[index].Key.Equals(name, StringComparison.Ordinal)) return GetObject(index, redacted);
            throw new KeyNotFoundException();
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and string representation of the object.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to hide sensitive data.</param>
        /// <returns>The key-value pair describing a property name and string representation of the object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> GetObjectAsString(int index, bool redacted)
        {
            var pair = GetObject(index, redacted);
            var stringRepresentation = Format(null, pair.Value, this);
            return KeyValuePair.Create(pair.Key, stringRepresentation);
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and string representation of the object.
        /// </summary>
        /// <param name="name">The property name to find.</param>
        /// <param name="redacted">Indicates whether need to hide sensitive data.</param>
        /// <returns>The key-value pair describing a property name and string representation of the object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="name"/> is not found.</exception>
        public KeyValuePair<string, string> GetObjectAsString(string name, bool redacted)
        {
            var pair = GetObject(name, redacted);
            var stringRepresentation = Format(null, pair.Value, this);
            return KeyValuePair.Create(pair.Key, stringRepresentation);
        }
        /// <summary>
        /// Checks whether the property of the specified type belongs to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="MessageTemplate"/></term>
        ///         <description>The property name of the message template.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="DictionaryEntry"/></term>
        ///         <description>The string representation of the dictionary entry key.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="type">The sensetive key type.</param>
        /// <param name="property">The property whose value is belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if property of the specified type belongs to sensitive data; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> or <paramref name="property"/> is <see langword="null"/>.</exception>
        public bool IsSensitiveData(Type type, string property)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(property);
            return _sensitiveDataType.TryGetValue(type, out var hashset) && hashset.Contains(property);
        }
        /// <summary>
        /// Registers a property whose value belongs to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="MessageTemplate"/></term>
        ///         <description>The property name of the message template.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="DictionaryEntry"/></term>
        ///         <description>The string representation of the dictionary entry key.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="type">The sensetive key type.</param>
        /// <param name="property">The property whose value is belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> or <paramref name="property"/> is <see langword="null"/>.</exception>
        public bool RegisterSensitiveData(Type type, string property)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(property);
            return _sensitiveDataType.TryGetValue(type, out var hashset) ? hashset.Add(property) : _sensitiveDataType.TryAdd(type, [property]);
        }
        /// <summary>
        /// Registers an array of properties whose values belong to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="MessageTemplate"/></term>
        ///         <description>The property name of the message template.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="DictionaryEntry"/></term>
        ///         <description>The string representation of the dictionary entry key.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="type">The sensetive key type.</param>
        /// <param name="args">An array of properties whose value belongs to sensitive data.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> or at least one element in the specified array is <see langword="null"/></exception>
        public int RegisterSensitiveData(Type type, params string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!Array.TrueForAll(args, x => x is not null))
                throw new ArgumentNullException(nameof(args), "At least one element in the specified array was null.");

            var count = 0;
            foreach (var name in args)
                count += Convert.ToInt32(RegisterSensitiveData(type, name));
            return count;
        }
        /// <summary>
        /// Registers an array of properties whose values belong to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="MessageTemplate"/></term>
        ///         <description>The property name of the message template.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="DictionaryEntry"/></term>
        ///         <description>The string representation of the dictionary entry key.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="args">The configuration of the sensitive formatter.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> or at least one element in the specified array is <see langword="null"/>.</exception>
        public int RegisterSensitiveData(IEnumerable<KeyValuePair<Type, HashSet<string>>> args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!Array.TrueForAll([.. args], x => x.Key is not null || Array.TrueForAll([.. x.Value], x => x is not null)))
                throw new ArgumentNullException(nameof(args), "At least one element in the specified array was null.");

            var count = 0;
            foreach (var arg in args)
                count += RegisterSensitiveData(arg.Key, [.. arg.Value]);
            return count;
        }
        /// <inheritdo1c/>
        public override string ToString()
        {
            return _messageTemplate is not null ? _messageTemplate.Format(this, TakeBySegmentOrder(_messageTemplate)) : NullFormat;

            object?[] TakeBySegmentOrder(MessageTemplate messageTemplate)
            {
                var dictionary = new Dictionary<string, object?>();
                foreach (var segment in messageTemplate)
                {
                    if (dictionary.ContainsKey(segment.Name)) continue;
                    dictionary[segment.Name] = GetObject(segment.Name, true).Value;
                }
                return [.. dictionary.Values];
            }
        }
    }
}