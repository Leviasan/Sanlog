﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

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
        /// The operator in front of the argument name tells the formatter to serialize the object passed in, rather than convert it using ToString().
        /// </summary>
        public const string SerializeOperator = "@";
        /// <summary>
        /// The message format of the redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";
        /// <summary>
        /// The message format that represents a primitive type array.
        /// </summary>
        public static readonly CompositeFormat CollapsePrimitiveFormat = CompositeFormat.Parse("[*{0} {1}*]");

        /// <summary>
        /// The key value pair collection to format.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyCollection<KeyValuePair<string, object?>> _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        /// <summary>
        /// The configuration of the formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SensitiveFormatterOptions? _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveFormatter"/> class.
        /// </summary>
        public SensitiveFormatter() : this([]) { }

        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }
        /// <summary>
        /// Gets or sets the configuration of the formatter.
        /// </summary>
        [AllowNull]
        public SensitiveFormatterOptions SensitiveConfiguration
        {
            get => _configuration ??= new SensitiveFormatterOptions();
            set => _configuration = value;
        }

        #region Interface: ICustomFormatter
        /// <inheritdoc/>
        public virtual string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (format is not null && format.Length == 1 && format.Equals(SerializeOperator, StringComparison.Ordinal) && arg is not null)
            {
                var props = arg.GetType().GetProperties();
                var nodes = props.Select(x => KeyValuePair.Create(x.Name, x.GetValue(arg))).ToArray();
                var stringBuilder = new StringBuilder(256).Append('{');
                for (var index = 0; index < nodes.Length; ++index)
                {
                    var node = nodes[index];
                    _ = stringBuilder.Append(' ').Append(node.Key).Append(' ').Append('=').Append(' ').Append(Format(null, node.Value, this));
                    _ = index < nodes.Length - 1 ? stringBuilder.Append(',') : stringBuilder.Append(' ');
                }
                return stringBuilder.Append('}').ToString();
            }
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
            for (var index = 0; index < _collection.Count; ++index)
            {
                if (EqualOrdinal(_collection.ElementAt(index).Key, key))
                {
                    return index;
                }
            }
            return -1;

            static bool EqualOrdinal(ReadOnlySpan<char> left, ReadOnlySpan<char> rigth)
                => left.Equals(rigth, StringComparison.Ordinal) || (left.Length > 1 && left.StartsWith(SerializeOperator, StringComparison.Ordinal) && left[1..].Equals(rigth, StringComparison.Ordinal));
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
            var newkey = kvp.Key[(kvp.Key.StartsWith(SerializeOperator, StringComparison.Ordinal) ? 1 : 0)..];
            var newvalue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
            return KeyValuePair.Create(newkey, newvalue);
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
            var newkey = kvp.Key[(kvp.Key.StartsWith(SerializeOperator, StringComparison.Ordinal) ? 1 : 0)..];
            var newvalue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
            return KeyValuePair.Create(newkey, newvalue);
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
            if (redacted && SensitiveConfiguration.IsSensitive(SensitiveKeyType.SegmentName, key))
            {
                return RedactedValue;
            }
            else if (key.StartsWith(SerializeOperator, StringComparison.Ordinal) && value is not null)
            {
                return Format(SerializeOperator, value, this);
            }
            return SensitiveObject(value, redacted);

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
                        var newvalue = redacted && SensitiveConfiguration.IsSensitive(SensitiveKeyType.DictionaryEntry, newkey) ? RedactedValue : SensitiveObject(entry.Value, redacted);
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
                        {
                            return redacted && SensitiveConfiguration.IsSensitive(SensitiveKeyType.CollapsePrimitive, key)
                                ? ArrayToFormat(enumerable, elementType, this)
                                : enumerable;
                        }
                    }
                    else if (type.IsGenericType && type.GenericTypeArguments.Length == 1)
                    {
                        var elementType = type.GenericTypeArguments.First();
                        if (elementType.IsPrimitive)
                        {
                            return redacted && SensitiveConfiguration.IsSensitive(SensitiveKeyType.CollapsePrimitive, key)
                                ? ArrayToFormat(enumerable, elementType, this)
                                : enumerable;
                        }
                    }
                    var newlist = new ArrayList();
                    foreach (var value in enumerable)
                        _ = newlist.Add(SensitiveObject(value, redacted));
                    return newlist;
                }
                static string ArrayToFormat(IEnumerable enumerable, Type type, IFormatProvider? formatProvider)
                {
                    return string.Format(formatProvider, CollapsePrimitiveFormat, IEnumerableCount(enumerable), type.Name);

                    static int IEnumerableCount(IEnumerable enumerable)
                    {
                        var count = 0;
                        foreach (var item in enumerable)
                            ++count;
                        return count;
                    }
                }
            }
        }
    }
}