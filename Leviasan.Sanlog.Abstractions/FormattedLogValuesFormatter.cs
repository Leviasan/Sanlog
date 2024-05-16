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
    /// Represents the formatter of the <see cref="Microsoft.Extensions.Logging.FormattedLogValues"/> class.
    /// Provides a mechanism to parse named format string to composite string.
    /// </summary>
    public sealed class FormattedLogValuesFormatter : IReadOnlyList<KeyValuePair<string, string>>
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
        /// The delimiters used by NamedFormat composite string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly char[] FormatDelimiters = [',', ':'];

        /// <summary>
        /// Max cached collection size.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private const int MaxCachedCollectionSize = 1024;
        /// <summary>
        /// The cache collection of the composite strings.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<string, CompositeFormat> CachedCompositeFormat = [];
        /// <summary>
        /// The cache collection of the value names.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Dictionary<string, IReadOnlyList<string>> CachedValueNames = [];

        /// <summary>
        /// The original property values.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<KeyValuePair<string, object?>> _logValues;
        /// <summary>
        /// The name collection of the named item format properties that belong to sensitive data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly HashSet<string> _sensitiveDataType = [];
        /// <summary>
        /// The function to get a string representation of the object.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Func<string?>? _formatter;
        /// <summary>
        /// An object that supplies culture-specific formatting information.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IFormatProvider? _formatProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the original property values and name collection of the named item format properties that belong to sensitive data.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information. If the passed value is <see langword="null"/> it's the same as using <see cref="CultureInfo.CurrentCulture"/>.</param>
        /// <param name="original">The original property values.</param>
        /// <param name="formatter">The function to get string representation of the object if <paramref name="original"/> does not contains key <see cref="OriginalFormat"/>.</param>
        public FormattedLogValuesFormatter(IFormatProvider? formatProvider, IReadOnlyList<KeyValuePair<string, object?>>? original, Func<string?>? formatter)
        {
            _formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
            _logValues = original ?? [];
            _formatter = formatter;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatter"/> class with the specified named format string to parse and an object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information. If the passed value is <see langword="null"/> it's the same as using <see cref="CultureInfo.CurrentCulture"/>.</param>
        /// <param name="format">The named format string to parse.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="format"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in format is invalid.</exception>
        public FormattedLogValuesFormatter(IFormatProvider? formatProvider, string format, params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(format);
            ArgumentNullException.ThrowIfNull(args);

            TryGetValueOrParseNamedFormat(format, out _, out var valueNames);
            var original = new List<KeyValuePair<string, object?>>();
            for (var index = 0; index < args.Length; ++index)
                original.Add(new KeyValuePair<string, object?>(valueNames[index], args[index]));
            original.Add(new KeyValuePair<string, object?>(OriginalFormat, format));

            _logValues = original;
            _formatProvider = formatProvider ?? CultureInfo.CurrentCulture;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> this[int index] => GetData(index, true);
        /// <summary>
        /// Gets the element value at the specified key in the read-only list.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <returns>The element value at the specified key in the read-only list.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="key"/> not found.</exception>
        public string this[string key] => GetData(key, true).Value;
        /// <inheritdoc/>
        public int Count => _logValues.Count;

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            for (var index = 0; index < Count; ++index) yield return this[index];
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// Gets the key-value pair at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need redact value after extracting.</param>
        /// <returns>The key-value pair at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> GetData(int index, bool redacted)
        {
            return new KeyValuePair<string, string>(_logValues[index].Key, redacted && IsSensitiveData(_logValues[index].Key) ? RedactedValue : FormatArgument(_formatProvider, _logValues[index].Value));

            // Summary: Formats the specified value to a string representation
            // Param (formatProvider): An object that supplies culture-specific formatting information
            // Param (value): An object to format
            // Returns: A string representation of the formatted object
            static string FormatArgument(IFormatProvider? formatProvider, object? value) => TryFormatArgument(formatProvider, value, out var stringValue) ? stringValue : Convert.ToString(value ?? NullValue, formatProvider)!;
            // Summary: Tries to format the specified value to a string representation
            // Param (formatProvider): An object that supplies culture-specific formatting information
            // Param (value): An object to format
            // Param out (stringValue): A string representation of the formatted object if the operation is successful; otherwise null
            // Returns: true if the format operation is successful; otherwise false
            static bool TryFormatArgument<T>(IFormatProvider? formatProvider, T? value, [NotNullWhen(true)] out string? stringValue)
            {
                // If the value is DateTime using a sortable date/time pattern ("s") defined in ISO 8601
                if (value is DateTime dateTime)
                {
                    stringValue = dateTime.ToString("s", formatProvider);
                    return true;
                }
                // If the value is DateTimeOffset using a sortable date/time pattern ("s") defined in ISO 8601
                else if (value is DateTimeOffset dateTimeOffset)
                {
                    stringValue = dateTimeOffset.ToString("s", formatProvider);
                    return true;
                }
                // If the value implements IDictionary builds a comma-separated string in KeyValuePair->ToString style
                else if (value is IDictionary dictionary)
                {
                    var first = true;
                    var stringBuilder = new StringBuilder(256);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        if (!first) _ = stringBuilder.Append(", ");
                        _ = stringBuilder.Append(formatProvider, $"[{Convert.ToString(entry.Key, formatProvider)}, {Convert.ToString(entry.Value ?? NullValue, formatProvider)}]");
                        first = false;
                    }
                    stringValue = stringBuilder.ToString();
                    return true;
                }
                // If the value implements IEnumerable but isn't itself a string, build a comma separated string
                else if (value is not string and IEnumerable enumerable)
                {
                    var first = true;
                    var stringBuilder = new StringBuilder(256);
                    foreach (var e in enumerable)
                    {
                        if (!first) _ = stringBuilder.Append(", ");
                        _ = stringBuilder.Append(Convert.ToString(e ?? NullValue, formatProvider));
                        first = false;
                    }
                    stringValue = stringBuilder.ToString();
                    return true;
                }
                stringValue = default;
                return false;
            }
        }
        /// <summary>
        /// Gets the key-value pair at the specified key.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <param name="redacted">Indicates whether need redact value after extracting.</param>
        /// <returns>The key-value pair at the specified key.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="key"/> not found.</exception>
        public KeyValuePair<string, string> GetData(string key, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(key);
            for (var index = 0; index < _logValues.Count; ++index)
                if (_logValues[index].Key == key) return GetData(index, redacted);
            throw new KeyNotFoundException();
        }
        /// <summary>
        /// Checks whether a value of the assotiated key of the <see cref="KeyValuePair{TKey, TValue}"/> belongs to sensitive data.
        /// </summary>
        /// <param name="key">The key of the <see cref="KeyValuePair{TKey, TValue}"/>.</param>
        /// <returns><see langword="true"/> if the value of the property belongs to sensitive data; otherwise <see langword="false"/>.</returns>
        public bool IsSensitiveData(string key) => _sensitiveDataType.Contains(key);
        /// <summary>
        /// Registers a name of the named item format property whose associated value will be redacted before logging.
        /// </summary>
        /// <param name="key">The name of the named item format property that belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        public bool RegisterSensitiveData(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return key != OriginalFormat && _sensitiveDataType.Add(key);
        }
        /// <summary>
        /// Registers name collection of the named item format properties whose associated values will be redacted before logging.
        /// </summary>
        /// <param name="keys">The name collection of the named item format properties that belong to sensitive data.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="keys"/> or one of the elements in array is <see langword="null"/>.</exception>
        public void RegisterSensitiveData(IEnumerable<string> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);
            foreach (var key in keys)
            {
                if (key is null) throw new ArgumentNullException(nameof(keys));
                _ = RegisterSensitiveData(key);
            }
        }
        /// <summary>
        /// Returns a string representation of the current instance.
        /// </summary>
        /// <returns>A string representation of the current instance.</returns>
        /// <exception cref="FormatException">A format item in format is invalid.</exception>
        public override string? ToString()
        {
            if (_logValues.SingleOrDefault(x => x.Key == OriginalFormat).Value is string originalFormat)
            {
                TryGetValueOrParseNamedFormat(originalFormat, out var compositeFormat, out var valueNames);
                var stringValue = compositeFormat.MinimumArgumentCount <= _logValues.Count
                    ? string.Format(_formatProvider, compositeFormat, TakeByValueNamesOrder(valueNames))
                    : compositeFormat.Format;
                return stringValue;
            }
            return _formatter is not null ? _formatter.Invoke() : NullFormat;

            // Summary: Returns a sequence of redacted values based on an order of value names
            // Param (valueNames): A sequence of the names in the expression
            // Returns: A sequence of the redacted values order by value names
            ReadOnlySpan<object?> TakeByValueNamesOrder(IReadOnlyList<string> valueNames)
            {
                var array = new string[valueNames.Count];
                for (var index = 0; index < array.Length; ++index)
                    array[index] = this[valueNames[index]];
                return array;
            }
        }
        /// <summary>
        /// Parses the specified named format string and tries to save it to the cache.
        /// </summary>
        /// <param name="format">The named format string to parse.</param>
        /// <param name="compositeFormat">The parsed composite string.</param>
        /// <param name="valueNames">The value names collection.</param>
        /// <exception cref="FormatException">A format item in format is invalid.</exception>
        private static void TryGetValueOrParseNamedFormat(string format, out CompositeFormat compositeFormat, out IReadOnlyList<string> valueNames)
        {
            Debug.Assert(format is not null);
            if (!CachedCompositeFormat.TryGetValue(format, out compositeFormat!))
            {
                (compositeFormat, valueNames) = ParseNamedFormat(format);
                if (compositeFormat.MinimumArgumentCount > 0 && CachedCompositeFormat.Count <= MaxCachedCollectionSize)
                {
                    CachedCompositeFormat.Add(format, compositeFormat);
                    CachedValueNames.Add(format, valueNames);
                }
            }
            else
            {
                valueNames = CachedValueNames[format];
            }

            // Summary: Parses the specified named format string to composite format string.
            // Exception: System.FormatException - A format item in format is invalid.
            static (CompositeFormat CompositeFormat, IReadOnlyList<string> ValueNames) ParseNamedFormat(string format)
            {
                var scanIndex = 0;
                var endIndex = format.Length;
                var stringBuilder = new StringBuilder(256);
                var valueNames = new List<string>();
                while (scanIndex < endIndex)
                {
                    var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                    if (scanIndex == 0 && openBraceIndex == endIndex)
                    {
                        return (CompositeFormat.Parse(format), valueNames);
                    }
                    var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);
                    if (closeBraceIndex == endIndex)
                    {
                        _ = stringBuilder.Append(format.AsSpan(scanIndex, endIndex - scanIndex));
                        scanIndex = endIndex;
                    }
                    else
                    {
                        // Format item syntax : { index[,alignment][ :formatString] }.
                        var formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);
                        _ = stringBuilder.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                        _ = stringBuilder.Append(valueNames.Count);
                        _ = stringBuilder.Append(format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1));
                        scanIndex = closeBraceIndex + 1;
                        valueNames.Add(format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1));
                    }
                }
                return (CompositeFormat.Parse(stringBuilder.ToString()), valueNames);

                // Summary: Reports the zero-based index of the first occurrence in string instance of brace in a specified array of Unicode characters.
                // The search starts at a specified character position and examines a specified number of character positions.
                static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
                {
                    // Example: {{prefix{{{Argument}}}suffix}}.
                    var braceIndex = endIndex;
                    var scanIndex = startIndex;
                    var braceOccurrenceCount = 0;
                    while (scanIndex < endIndex)
                    {
                        if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
                        {
                            if (braceOccurrenceCount % 2 == 0)
                            {
                                // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'.
                                braceOccurrenceCount = 0;
                                braceIndex = endIndex;
                            }
                            else
                            {
                                // An unescaped '{' or '}' found.
                                break;
                            }
                        }
                        else if (format[scanIndex] == brace)
                        {
                            if (brace == '}')
                            {
                                if (braceOccurrenceCount == 0)
                                    // For '}' pick the first occurrence.
                                    braceIndex = scanIndex;
                            }
                            else
                            {
                                // For '{' pick the last occurrence.
                                braceIndex = scanIndex;
                            }
                            braceOccurrenceCount++;
                        }
                        scanIndex++;
                    }
                    return braceIndex;
                }
                // Summary: Reports the zero-based index of the first occurrence in string instance of any character in a specified array of Unicode characters.
                // The search starts at a specified character position and examines a specified number of character positions.
                static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
                {
                    var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
                    return findIndex == -1 ? endIndex : findIndex;
                }
            }
        }
    }
}