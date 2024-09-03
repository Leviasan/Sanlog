using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of objects considering redact sensitive data.
    /// </summary>
    public class SensitiveFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// The message format that represents a redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";

        /// <summary>
        /// The raw values.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyDictionary<string, object?> _dictionary;
        /// <summary>
        /// The configuration of the sensitive data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SensitiveConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveFormatter"/> class with the specified raw values.
        /// </summary>
        /// <param name="dictionary">The raw values.</param>
        /// <param name="configuration">The configuration of the sensitive data.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="dictionary"/> is <see langword="null"/>.</exception>
        public SensitiveFormatter(IReadOnlyDictionary<string, object?> dictionary, SensitiveConfiguration? configuration)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _configuration = configuration ?? new SensitiveConfiguration();
        }

        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }
        /// <summary>
        /// Gets the configuration of the sensitive data.
        /// </summary>
        public SensitiveConfiguration SensitiveConfiguration => _configuration;

        #region Interface: IFormatProvider
        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : CultureInfo?.GetFormat(formatType);
        #endregion

        #region Interface: ICustomFormatter
        /// <inheritdoc/>
        public virtual string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (Equals(formatProvider))
            {
                formatProvider = CultureInfo;
            }
            return arg switch
            {
                IFormattable formattable => formattable.ToString(format, formatProvider),
                _ => Convert.ToString(arg, formatProvider) ?? string.Empty
            };
        }
        #endregion

        /// <summary>
        /// Determines whether the raw values contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><see langword="true"/> if the raw values contains an element that has the specified key; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);
        /// <summary>
        /// Gets a key-value pair considering the concealment of confidential data.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The key-value pair that describes a key and object considering the concealment of confidential data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in source.</exception>
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var kvp = _dictionary.ElementAt(index); // ArgumentOutOfRangeException
            var newvalue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
            return KeyValuePair.Create(kvp.Key, newvalue);
        }
        /// <summary>
        /// Gets a key-value pair considering the concealment of confidential data.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The key-value pair that describes a key and object considering the concealment of confidential data.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="key"/> does not exist in the collection.</exception>
        public KeyValuePair<string, object?> GetObject(string key, bool redacted)
        {
            var value = _dictionary[key]; // ArgumentNullException + KeyNotFoundException
            var newvalue = ProcessSensitiveObject(key, value, redacted);
            return KeyValuePair.Create(key, newvalue);
        }
        /// <summary>
        /// Processes a value through the sensitive formatter.
        /// </summary>
        /// <param name="key">The key of the value.</param>
        /// <param name="value">The value to process.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>A new value considering the concealment of confidential data.</returns>
        private object? ProcessSensitiveObject(string key, object? value, bool redacted)
        {
            return redacted && SensitiveConfiguration.Contains(typeof(object), key) ? RedactedValue : SensitiveObject(value, redacted);

            object? SensitiveObject(object? value, bool redacted)
            {
                return value switch
                {
                    string stringValue => stringValue, // string implements IEnumerable so must be process before
                    IDictionary dictionary => SensitiveDictionary(dictionary, redacted), // IDictionary implements IEnumerable so must be process before
                    IEnumerable<byte> byteArray => value, // IEnumerable<byte> implements IEnumerable so must be process before
                    IEnumerable enumerable => SensitiveEnumerable(enumerable, redacted),
                    _ => value
                };
                IDictionary SensitiveDictionary(IDictionary dictionary, bool redacted)
                {
                    var newdict = new Dictionary<object, object?>(dictionary.Count);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var key = Format(null, entry.Key, this);
                        var newvalue = redacted && SensitiveConfiguration.Contains(typeof(DictionaryEntry), key) ? RedactedValue : entry.Value;
                        newdict.Add(entry.Key, newvalue);
                    }
                    return newdict;
                }
                IEnumerable SensitiveEnumerable(IEnumerable enumerable, bool redacted)
                {
                    var newlist = new List<object?>();
                    foreach (var value in enumerable)
                        newlist.Add(SensitiveObject(value, redacted));
                    return newlist;
                }
            }
        }
        /// <summary>
        /// Processes all values through the sensitive formatter.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, object?>> ToEnumerable() => _dictionary.Select(element => GetObject(element.Key, true));
    }
}