using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Sanlog
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of objects considering redact sensitive data.
    /// </summary>
    public class SensitiveFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// The message format of the redacted value.
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
        /// Initializes a new instance of the <see cref="SensitiveFormatter"/> class with the specified raw values and configuration of the sensitive data.
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
        /// Determines whether the dictionary contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><see langword="true"/> if the dictionary contains an element that has the specified key; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);
        /// <summary>
        /// Returns the element at a specified index in a sequence.
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
        /// Returns the element with a specified name in a sequence.
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
        /// Processes a value through the sensitive formatter.
        /// </summary>
        /// <param name="key">The key.</param>
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
                        var newvalue = redacted && SensitiveConfiguration.Contains(typeof(DictionaryEntry), newkey) ? RedactedValue : entry.Value;
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
        /// <summary>
        /// Projects each element processes through the formatter into a string through invoke <see cref="Format(string?, object?, IFormatProvider?)"/>.
        /// </summary>
        /// <returns>A dictionary whose elements result from invoking the transform function <see cref="Format(string?, object?, IFormatProvider?)"/> on each element.</returns>
        public Dictionary<string, string?> ToDictionary() => ToDictionary(selector => Format(null, selector, this));
        /// <summary>
        /// Projects each element processes through the formatter into a new form.
        /// </summary>
        /// <param name="selector">A transform function to apply to each element.</param>
        /// <typeparam name="TResult">The type of the value returned by selector.</typeparam>
        /// <returns>A dictionary whose elements result from invoking the transform function on each element.</returns>
        private Dictionary<string, TResult?> ToDictionary<TResult>(Func<object?, TResult?> selector)
        {
            Debug.Assert(selector is not null);
            return _dictionary
                .Select(x => GetObject(x.Key, true))
                .ToDictionary(ks => ks.Key, es => selector.Invoke(es.Value));
        }
    }
}