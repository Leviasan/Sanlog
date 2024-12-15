using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Sanlog
{
    /// <summary>
    /// Represents a formatter that supports the concealment of confidential data.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SensitiveFormatter"/> class with the specified object array that contains zero or more objects to format. 
    /// </remarks>
    /// <param name="dictionary">An object array that contains zero or more objects to format.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public class SensitiveFormatter(IReadOnlyDictionary<string, object?> dictionary) : IFormatProvider, ICustomFormatter, IEnumerable<KeyValuePair<string, object?>>
    {
        /// <summary>
        /// The message format of the redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";

        /// <summary>
        /// The configuration of the sensitive data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SensitiveConfiguration _configuration = new();
        /// <summary>
        /// An object dictionary.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyDictionary<string, object?> _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveFormatter"/> class with the specified object array that contains zero or more objects to format.
        /// </summary>
        /// <param name="enumerable">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="enumerable"/> is <see langword="null"/>.</exception>
        public SensitiveFormatter(IEnumerable<object?> enumerable)
            : this(enumerable?.Select((element, index) => KeyValuePair.Create($"args[{index}]", element)).ToDictionary() ?? throw new ArgumentNullException(nameof(enumerable))) { }

        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }
        /// <summary>
        /// Gets the configuration of the sensitive data.
        /// </summary>
        public SensitiveConfiguration SensitiveConfiguration
        {
            get => _configuration;
            set => _configuration = value ?? throw new ArgumentNullException(nameof(SensitiveConfiguration));
        }

        #region Interface: ICustomFormatter
        /// <inheritdoc/>
        public virtual string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            var provider = Equals(formatProvider) ? CultureInfo : formatProvider;
            return arg switch
            {
                IFormattable formattable => formattable.ToString(format, provider),
                _ => Convert.ToString(arg, provider) ?? string.Empty
            };
        }
        #endregion

        #region Interface: IEnumerable
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var index = 0; index < _dictionary.Count; ++index)
                yield return GetObject(index, true);
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Interface: IFormatProvider
        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : CultureInfo?.GetFormat(formatType);
        #endregion

        /// <summary>
        /// Determines whether the formatter contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><see langword="true"/> if the formatter contains an element that has the specified key; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);
        /// <summary>
        /// Returns the element at a specified index in the sequence.
        /// </summary>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in the source.</exception>
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var kvp = _dictionary.ElementAt(index); // ArgumentOutOfRangeException
            var newvalue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
            return KeyValuePair.Create(kvp.Key, newvalue);
        }
        /// <summary>
        /// Returns the element with the specified key in a sequence.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element with the specified name in the source sequence.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="key"/> does not exist in the dictionary.</exception>
        public KeyValuePair<string, object?> GetObject(string key, bool redacted)
        {
            var value = _dictionary[key]; // ArgumentNullException + KeyNotFoundException
            var newvalue = ProcessSensitiveObject(key, value, redacted);
            return KeyValuePair.Create(key, newvalue);
        }
        /// <summary>
        /// Returns the string representation of the element at a specified index in a sequence.
        /// </summary>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The string representation of the element at a specified index in a sequence.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Index was outside the bounds of the array.</exception>
        public KeyValuePair<string, string> GetObjectAsString(int index, bool redacted)
        {
            var pair = GetObject(index, redacted); // ArgumentOutOfRangeException
            var stringRepresentation = Format(null, pair.Value, this);
            return KeyValuePair.Create(pair.Key, stringRepresentation);
        }
        /// <summary>
        /// Returns the string representation of the element with the specified key in a sequence.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The string representation of the element with the specified key in a sequence.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">The <paramref name="key"/> is not found.</exception>
        public KeyValuePair<string, string> GetObjectAsString(string key, bool redacted)
        {
            var pair = GetObject(key, redacted); // ArgumentNullException + KeyNotFoundException
            var stringRepresentation = Format(null, pair.Value, this);
            return KeyValuePair.Create(pair.Key, stringRepresentation);
        }
        /// <summary>
        /// Processes a value through the sensitive formatter.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to process.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>A new value considering the concealment of confidential data.</returns>
        private object? ProcessSensitiveObject(string key, object? value, bool redacted)
        {
            return redacted && SensitiveConfiguration.Contains(SensitiveItemType.Segment, key) ? RedactedValue : SensitiveObject(value, redacted);

            object? SensitiveObject(object? value, bool redacted)
            {
                return value switch
                {
                    string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => SensitiveDictionary(dictionary, redacted), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => SensitiveEnumerable(enumerable, redacted),
                    _ => value
                };
                IDictionary SensitiveDictionary(IDictionary dictionary, bool redacted)
                {
                    var newdict = new Dictionary<string, object?>(dictionary.Count);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var newkey = Format(null, entry.Key, this);
                        var newvalue = redacted && SensitiveConfiguration.Contains(SensitiveItemType.DictionaryEntry, newkey) ? RedactedValue : entry.Value;
                        newdict.Add(newkey, newvalue);
                    }
                    return newdict;
                }
                IEnumerable SensitiveEnumerable(IEnumerable enumerable, bool redacted)
                {
                    var type = enumerable.GetType();
                    if (type.IsArray)
                    {
                        var elementType = type.GetElementType();
                        if (elementType is not null && elementType.IsPrimitive)
                            return enumerable;
                    }
                    else if (type.IsGenericType && type.GenericTypeArguments.Length == 1)
                    {
                        var elementType = type.GenericTypeArguments.First();
                        if (elementType.IsPrimitive)
                            return enumerable;
                    }
                    var newlist = new ArrayList();
                    foreach (var value in enumerable)
                        _ = newlist.Add(SensitiveObject(value, redacted));
                    return newlist;
                }
            }
        }
    }
}