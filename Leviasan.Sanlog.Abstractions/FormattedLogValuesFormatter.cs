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
    /// Represents the formatter of the Microsoft.Extensions.Logging.FormattedLogValues class.
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
        private const int MaxCachedFormatters = 1024; // Microsoft.Extensions.Logging.FormattedLogValues.MaxCachedFormatters
        /// <summary>
        /// The cache of the named format strings.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<string, NamedFormatString> CachedNamedFormatStrings = [];
        /// <summary>
        /// Tries to get a named format string from the cache or parses and tries to add to cache it.
        /// </summary>
        /// <param name="format">The key of the element to get or add.</param>
        /// <param name="namedFormatString">When this method returns, it contains a parsed named format string or the <see langword="null"/> if the operation failed.</param>
        /// <returns><see langword="true"/> if operation is successful; otherwise <see langword="false"/>.</returns>
        /// <exception cref="FormatException">A format item in format is invalid.</exception>
        private static bool TryGetOrAdd(string? format, [NotNullWhen(true)] out NamedFormatString? namedFormatString)
        {
            if (!string.IsNullOrEmpty(format))
            {
                if (!CachedNamedFormatStrings.TryGetValue(format, out namedFormatString))
                {
                    namedFormatString = NamedFormatString.Parse(format); // FormatException
                    return namedFormatString.CompositeFormat.MinimumArgumentCount <= 0 || CachedNamedFormatStrings.Count >= MaxCachedFormatters || CachedNamedFormatStrings.TryAdd(format, namedFormatString);
                }
                return true;
            }
            namedFormatString = default;
            return false;
        }

        /// <summary>
        /// The hash set of the sensitive segment's names.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HashSet<string> _sensitiveData = [];
        /// <summary>
        /// The original values.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<KeyValuePair<string, object?>> _originalValues;
        /// <summary>
        /// An object that supplies culture-specific formatting information.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IFormatProvider? _formatProvider;
        /// <summary>
        /// The parsed named format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly NamedFormatString? _namedFormatString;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with an array of original values and an object that supplies culture-specific formatting information.
        /// </summary>
        /// <param name="original">An array of original values.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <exception cref="FormatException">A format item in format is invalid.</exception>
        /// <exception cref="InvalidOperationException">More than one <see cref="OriginalFormat"/> key was found at <paramref name="original"/> array.</exception>
        public FormattedLogValuesFormatter(IReadOnlyList<KeyValuePair<string, object?>>? original, IFormatProvider? formatProvider)
        {
            _originalValues = original ?? [];
            _formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
            _ = TryGetOrAdd(_originalValues.SingleOrDefault(x => x.Key == OriginalFormat).Value as string, out _namedFormatString); // InvalidOperationException + FormatException
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified named format string to parse and an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="cache">An object that supplies culture-specific formatting information.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <param name="format">The named format string to parse.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="format"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in format is invalid.</exception>
        public FormattedLogValuesFormatter(IFormatProvider? formatProvider, string format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);

            _formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
            if (TryGetOrAdd(format, out _namedFormatString)) // FormatException
            {
                var original = new List<KeyValuePair<string, object?>>();
                for (int index = 0, segmentId = 0; index < args.Length; ++index, ++segmentId)
                {
                    while (segmentId < _namedFormatString.Segments.Count && original.Any(x => x.Key == _namedFormatString.Segments[segmentId].Name))
                    {
                        ++segmentId;
                    }
                    original.Add(KeyValuePair.Create(_namedFormatString.Segments[segmentId].Name, args[index]));
                }
                original.Add(KeyValuePair.Create<string, object?>(OriginalFormat, format));
                _originalValues = original;
            }
            else
            {
                _originalValues = args.Select((element, index) => KeyValuePair.Create(index.ToString(null, _formatProvider), element)).ToList();
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> this[int index] => GetObjectAsString(index, true);
        /// <summary>
        /// Gets the string representation of the element value at the specified key.
        /// </summary>
        /// <param name="name">The property name of the element to get.</param>
        /// <returns>The element value at the specified key in the read-only list.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="name"/> not found.</exception>
        public KeyValuePair<string, string> this[string name] => GetObjectAsString(name, true);
        /// <inheritdoc/>
        public int Count => _originalValues.Count;
        /// <summary>
        /// Indicates whether <see cref="OriginalFormat"/> is registered.
        /// </summary>
        public bool HasOriginalFormat => _originalValues.Any(x => x.Key == OriginalFormat);

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
        /// <inheritdoc/>
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            // Override default format string component
            return string.IsNullOrEmpty(format) && TryFormatArgument(arg, formatProvider, out var stringValue) ? stringValue
                : arg is IFormattable formattable ? formattable.ToString(format, formatProvider)
                : Convert.ToString(arg ?? NullValue, formatProvider)!;

            static string FormatArgument(object? value, IFormatProvider? formatProvider)
            {
                return TryFormatArgument(value, formatProvider, out var stringValue) ? stringValue
                    : Convert.ToString(value ?? NullValue, formatProvider)!;
            }
            static bool TryFormatArgument(object? value, IFormatProvider? formatProvider, [NotNullWhen(true)] out string? stringValue)
            {
                stringValue = value switch
                {
                    DateTime dateTime => dateTime.ToString("O", formatProvider),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", formatProvider),
                    Enum @enum => @enum.ToString("D"),
                    float binary32 => binary32.ToString("G9", formatProvider),
                    double binary64 => binary64.ToString("G17", formatProvider),
                    IDictionary dictionary => DictionaryToString(dictionary, formatProvider),
                    string @string => @string, // Position before IEnumerable is important
                    IEnumerable enumerable => EnumerableToString(enumerable, formatProvider),
                    _ => null
                };
                return !string.IsNullOrEmpty(stringValue);
            }
            static string? DictionaryToString(IDictionary dictionary, IFormatProvider? formatProvider)
            {
                var first = true;
                StringBuilder? stringBuilder = null;
                foreach (DictionaryEntry entry in dictionary)
                {
                    stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                    stringBuilder = stringBuilder.Append(formatProvider, $"[{FormatArgument(entry.Key, formatProvider)}, {FormatArgument(entry.Value, formatProvider)}]");
                    first = false;
                }
                return stringBuilder?.Append(']').ToString();
            }
            static string? EnumerableToString(IEnumerable enumerable, IFormatProvider? formatProvider)
            {
                var first = true;
                StringBuilder? stringBuilder = null;
                foreach (var e in enumerable)
                {
                    stringBuilder = first ? new StringBuilder(256).Append('[') : stringBuilder!.Append(", ");
                    stringBuilder = stringBuilder.Append(FormatArgument(e, formatProvider));
                    first = false;
                }
                return stringBuilder?.Append(']').ToString();
            }
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and object.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need redact value.</param>
        /// <returns>The key-value pair describing a property name and object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var key = _originalValues[index].Key;
            return KeyValuePair.Create(key, redacted && IsSensitiveData(key) ? RedactedValue : _originalValues[index].Value);
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and object.
        /// </summary>
        /// <param name="name">The property name to find.</param>
        /// <param name="redacted">Indicates whether need redact value.</param>
        /// <returns>The key-value pair describing a property name and object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="name"/> is not found.</exception>
        public KeyValuePair<string, object?> GetObject(string name, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(name);
            for (var index = 0; index < _originalValues.Count; ++index)
                if (_originalValues[index].Key == name) return GetObject(index, redacted);
            throw new KeyNotFoundException();
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and string representation of the object.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need redact value.</param>
        /// <returns>The key-value pair describing a property name and string representation of the object.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> GetObjectAsString(int index, bool redacted)
        {
            var pair = GetObject(index, redacted);
            var formattedValue = Format(null, pair.Value, this);
            return KeyValuePair.Create(pair.Key, formattedValue);
        }
        /// <summary>
        /// Gets a key-value pair describing a property name and string representation of the object.
        /// </summary>
        /// <param name="name">The property name to find.</param>
        /// <param name="redacted">Indicates whether need redact value.</param>
        /// <returns>The key-value pair describing a property name and string representation of the object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="name"/> is not found.</exception>
        public KeyValuePair<string, string> GetObjectAsString(string name, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(name);
            for (var index = 0; index < _originalValues.Count; ++index)
                if (_originalValues[index].Key == name) return GetObjectAsString(index, redacted);
            throw new KeyNotFoundException();
        }
        /// <summary>
        /// Checks whether a segment name belongs to sensitive data.
        /// </summary>
        /// <param name="name">The name of the segment to check.</param>
        /// <returns><see langword="true"/> if the segment name belongs to sensitive data; otherwise <see langword="false"/>.</returns>
        public bool IsSensitiveData(string name) => _sensitiveData.Contains(name);
        /// <summary>
        /// Registers a segment name whose associated value will be redacted before logging.
        /// </summary>
        /// <param name="name">The name of the segment that belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is <see langword="null"/>.</exception>
        public bool RegisterSensitiveData(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            return name != OriginalFormat && _sensitiveData.Add(name);
        }
        /// <summary>
        /// Registers segment names whose associated value will be redacted before logging.
        /// </summary>
        /// <param name="names">The names of the segment that belongs to sensitive data.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="names"/> or at least one element in the specified array is <see langword="null"/>.</exception>
        public int RegisterSensitiveData(params string[] names)
        {
            ArgumentNullException.ThrowIfNull(names);
            if (!Array.TrueForAll(names, x => x is not null))
                throw new ArgumentNullException(nameof(names), "At least one element in the specified array was null.");

            var count = 0;
            foreach (var name in names)
                count += Convert.ToInt32(RegisterSensitiveData(name));
            return count;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return _namedFormatString is not null
                ? _namedFormatString.Format(_formatProvider, TakeBySegmentOrder(_namedFormatString.Segments))
                : NullFormat;

            // Summary: Returns a sequence of redacted values based on an order of segment names.
            // Param (segments): A sequence of the segments in the named format string.
            // Returns: A sequence of the redacted values order by segment names.
            object?[] TakeBySegmentOrder(IReadOnlyList<NamedFormatString.FormatSegment> segments)
            {
                var dictionary = new Dictionary<string, object?>();
                foreach (var segment in segments)
                {
                    if (dictionary.ContainsKey(segment.Name)) continue;
                    dictionary[segment.Name] = segments.Where(x => x.Name == segment.Name).All(x => string.IsNullOrEmpty(x.FormatString))
                        ? GetObjectAsString(segment.Name, true).Value
                        : GetObject(segment.Name, true).Value;
                }
                return [.. dictionary.Values];
            }
        }
    }
}