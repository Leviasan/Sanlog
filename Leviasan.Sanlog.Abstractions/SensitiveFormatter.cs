using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// 
    /// Represents the formatter that supports custom formatting of object.
    /// </summary>
    public abstract class SensitiveFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// The message format that represents a redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";

        /// <summary>
        /// The raw values.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyList<KeyValuePair<string, object?>> _dictionary;
        /// <summary>
        /// The configuration of the sensitive data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SensitiveConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveFormatter"/> class with the specified raw values and the configuration of the sensitive data.
        /// </summary>
        /// <param name="dictionary">The raw values.</param>
        /// <param name="configuration">The configuration of the sensitive data.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        protected SensitiveFormatter(IReadOnlyList<KeyValuePair<string, object?>> dictionary, SensitiveConfiguration configuration)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets or sets an object that supplies culture-specific formatting information.
        /// </summary>
        public IFormatProvider? FormatProvider { get; set; }

        /// <inheritdoc/>
        public abstract string Format(string? format, object? arg, IFormatProvider? formatProvider);
        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : FormatProvider?.GetFormat(formatType);
        /// <summary>
        /// Gets a key-value pair taking into account the concealment of confidential data.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The key-value pair that describes a key and object taking into account the concealment of confidential data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var key = _dictionary[index].Key; // ArgumentOutOfRangeException
            var value = ProcessSensitiveObject(key, _dictionary[index].Value, redacted);
            return KeyValuePair.Create(key, value);

            object? ProcessSensitiveObject(string key, object? value, bool redacted)
            {
                return redacted && _configuration.Contains(typeof(object), key) ? RedactedValue : SensitiveObject(value, redacted);

                object? SensitiveObject(object? value, bool redacted)
                {
                    return value switch
                    {
                        string stringValue => stringValue, // string implements IEnumerable so must be process before
                        IDictionary dictionary => SensitiveDictionary(dictionary, redacted), // IDictionary implements IEnumerable so must be process before
                        IEnumerable enumerable => SensitiveEnumerable(enumerable, redacted),
                        _ => value
                    };
                    IDictionary SensitiveDictionary(IDictionary dictionary, bool redacted)
                    {
                        var newdict = new Dictionary<object, object?>(dictionary.Count);
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            var key = Format(null, entry.Key, this);
                            var newvalue = redacted && _configuration.Contains(typeof(DictionaryEntry), key) ? RedactedValue : entry.Value;
                            newdict.Add(entry.Key, newvalue);
                        }
                        return newdict;
                    }
                    IEnumerable SensitiveEnumerable(IEnumerable enumerable, bool redacted)
                    {
                        var newlist = new List<object?>();
                        foreach (var entry in enumerable)
                            newlist.Add(SensitiveObject(entry, redacted));
                        return newlist;
                    }
                }
            }
        }
        /// <summary>
        /// Gets a key-value pair taking into account the concealment of confidential data.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The key-value pair that describes a key and object taking into account the concealment of confidential data.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="key"/> is not found.</exception>
        public KeyValuePair<string, object?> GetObject(string key, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(key);
            for (var index = 0; index < _dictionary.Count; ++index)
                if (_dictionary[index].Key.Equals(key, StringComparison.Ordinal)) return GetObject(index, redacted);
            throw new KeyNotFoundException();
        }
    }
}