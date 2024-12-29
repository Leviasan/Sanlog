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
    /// Initializes a new instance of the <see cref="SensitiveFormatter"/> class with the specified key value pair collection to format.
    /// </remarks>
    /// <param name="collection">The key value pair collection to format.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="collection"/> is <see langword="null"/>.</exception>
    public class SensitiveFormatter(IReadOnlyCollection<KeyValuePair<string, object?>> collection) : IFormatProvider, ICustomFormatter, IEnumerable<KeyValuePair<string, object?>>
    {
        /// <summary>
        /// The message format of the redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";

        /// <summary>
        /// The key value pair collection to format.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyCollection<KeyValuePair<string, object?>> _collection = collection ?? throw new ArgumentNullException(nameof(collection));

        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }
        /// <summary>
        /// Gets or sets the configuration of the sensitive data.
        /// </summary>
        public SensitiveConfiguration? SensitiveConfiguration { get; set; }

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
            for (var index = 0; index < _collection.Count; ++index)
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
        /// Searches for the specified key and returns the zero-based index of the first occurrence.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>The zero-based index of the first occurrence of item if found; otherwise, -1.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        public int IndexOf(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            for (var index = 0; index <= _collection.Count; ++index)
            {
                if (_collection.ElementAt(index).Key == key)
                {
                    return index;
                }
            }
            return -1;
        }
        /// <summary>
        /// Returns the element at a specified index in the sequence.
        /// </summary>
        /// <param name="index">The zero-based index of the element to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in the source.</exception>
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var kvp = _collection.ElementAt(index); // ArgumentOutOfRangeException
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
        /// <exception cref="InvalidOperationException">The <paramref name="key"/> does not exist in the sequence. -or- More than one element satisfies the condition. -or- The source sequence is empty.</exception>
        public KeyValuePair<string, object?> GetObject(string key, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(key);
            var kvp = _collection.Single(x => x.Key == key); // InvalidOperationException
            var newvalue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
            return KeyValuePair.Create(kvp.Key, newvalue);
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
        /// <exception cref="InvalidOperationException">The <paramref name="key"/> does not exist in the sequence. -or- More than one element satisfies the condition. -or- The source sequence is empty.</exception>
        public KeyValuePair<string, string> GetObjectAsString(string key, bool redacted)
        {
            var pair = GetObject(key, redacted); // ArgumentNullException + InvalidOperationException
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
            var configuration = SensitiveConfiguration ?? new SensitiveConfiguration();
            return redacted && configuration.Contains(SensitiveItemType.Segment, key) ? RedactedValue : SensitiveObject(value, redacted);

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
                        var newvalue = redacted && configuration.Contains(SensitiveItemType.DictionaryEntry, newkey) ? RedactedValue : entry.Value;
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