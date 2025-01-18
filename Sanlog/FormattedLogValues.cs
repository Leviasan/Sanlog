using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Sanlog
{
    /// <summary>
    /// Represents the wrapper of the Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    public sealed class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object?>>
    {
        /// <summary>
        /// The operator in front of the argument name tells the formatter to serialize the object passed in, rather than convert it using ToString().
        /// </summary>
        public const string OperatorSerialize = "@";
        /// <summary>
        /// The message format that represents a null format.
        /// </summary>
        public const string NullFormatValue = "[null]";
        /// <summary>
        /// The element key that represents a structured logging message.
        /// </summary>
        public const string OriginalFormatKey = "{OriginalFormat}";

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
                foreach (var segment in messageTemplate.Segments)
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
                dictionary.Add(OriginalFormatKey, format);
            }
            return dictionary;
        }

        /// <summary>
        /// The sensitive formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SensitiveFormatter? _sensitiveFormatter;
        /// <summary>
        /// The formatted log values formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FormattedLogValuesFormatter? _formattedLogValuesFormatter;
        /// <summary>
        /// The key value pair collection to format.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyCollection<KeyValuePair<string, object?>> _collection;
        /// <summary>
        /// The composite/named format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _format;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified key value pair collection to format.
        /// </summary>
        /// <param name="collection">The key value pair collection to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="collection"/> is <see langword="null"/>.</exception>
        public FormattedLogValues(IReadOnlyCollection<KeyValuePair<string, object?>> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _format = Convert.ToString(_collection.SingleOrDefault(x => x.Key.Equals(OriginalFormatKey, StringComparison.Ordinal)).Value, null);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified composite/named format string.
        /// </summary>
        /// <param name="format">The composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentException">Passed less than the minimum number of arguments that must be passed to a formatting operation.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        public FormattedLogValues(string? format, params object?[] args) : this(ParseCompositeArgs(format, args)) // ArgumentException + ArgumentNullException
            => _format = !string.IsNullOrEmpty(format) ? format : null;

        /// <summary>
        /// Gets or sets the configuration of the <see cref="SensitiveFormatter"/>.
        /// </summary>
        public SensitiveFormatterOptions? SensitiveConfiguration { get; set; }
        /// <summary>
        /// Gets or sets the configuration of the <see cref="FormattedLogValuesFormatter"/>.
        /// </summary>
        public FormattedLogValuesFormatterOptions? FormattedConfiguration { get; set; }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">Tne <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in source.</exception>
        public KeyValuePair<string, object?> this[int index] => this[index, true];
        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Tne <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in source.</exception>
        public KeyValuePair<string, object?> this[int index, bool redacted]
        {
            get
            {
                var kvp = _collection.ElementAt(index); // ArgumentOutOfRangeException
                var newkey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
                var newvalue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
                return KeyValuePair.Create(newkey, newvalue);
            }
        }
        /// <summary>
        /// Gets the element with the specified key in the read-only list.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The element with the specified key in the read-only list.</returns>
        /// <exception cref="InvalidOperationException">The <paramref name="key"/> does not exist in the sequence. -or- More than one element satisfies the condition. -or- The source sequence is empty.</exception>
        public KeyValuePair<string, object?> this[string key] => this[key, true];
        /// <summary>
        /// Gets the element with the specified key in the read-only list.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element with the specified key in the read-only list.</returns>
        /// <exception cref="InvalidOperationException">The <paramref name="key"/> does not exist in the sequence. -or- More than one element satisfies the condition. -or- The source sequence is empty.</exception>
        public KeyValuePair<string, object?> this[string key, bool redacted]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);
                var kvp = _collection.Single(x => x.Key == key); // InvalidOperationException
                var newkey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
                var newvalue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
                return KeyValuePair.Create(newkey, newvalue);
            }
        }
        /// <inheritdoc/>
        public int Count => _collection.Count;
        /// <summary>
        /// Indicates whether the original format is defined.
        /// </summary>
        public bool OriginalFormat => _format is not null;

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var index = 0; index < _collection.Count; ++index)
                yield return this[index, true];
        }
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public override string? ToString()
        {
            return TryGetOrAdd(_format, out var messageTemplate) ? messageTemplate.Format(_formattedLogValuesFormatter, TakeBySegmentOrder(messageTemplate)) : FormattedLogValuesFormatter.NullFormat;

            object?[] TakeBySegmentOrder(MessageTemplate messageTemplate)
            {
                var dictionary = new Dictionary<string, object?>();
                foreach (var segment in messageTemplate.Segments)
                {
                    if (dictionary.ContainsKey(segment))
                    {
                        continue;
                    }
                    var index = IndexOf(segment); // Defines the first occurrence of the key instead of directly using GetObject(string, bool) to prevent InvalidOperationException
                    dictionary[segment] = this[index].Value;
                }
                return [.. dictionary.Values];
            }
            int IndexOf(string key)
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
                    => left.Equals(rigth, StringComparison.Ordinal) || (left.Length > 1 && left.StartsWith(OperatorSerialize, StringComparison.Ordinal) && left[1..].Equals(rigth, StringComparison.Ordinal));
            }
        }
        /*
        /// <summary>
        /// Projects each element processes through the formatter into a string through invoke <see cref="Format(string?, object?, IFormatProvider?)"/>.
        /// </summary>
        /// <returns>An enumerable whose elements result from invoking the transform function <see cref="Format(string?, object?, IFormatProvider?)"/> on each element.</returns>
        */
        public IReadOnlyList<KeyValuePair<string, string?>> FormatToList() => this.Select(x => KeyValuePair.Create<string, string?>(x.Key, string.Format(_formattedLogValuesFormatter, "{0}", x.Value))).ToList();

        /// <summary>
        /// Processes a value through the sensitive formatter.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to process.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>A new value considering the concealment of confidential data.</returns>
        private object? ProcessSensitiveObject(string key, object? value, bool redacted)
        {
            var sensitive = _sensitiveFormatter ?? new SensitiveFormatter
            {
                CultureInfo = CultureInfo,
                Configuration = SensitiveConfiguration
            };

            if (redacted && sensitive.Configuration.IsSensitive(SensitiveKeyType.SegmentName, key))
            {
                return string.Format(_sensitiveFormatter, "{0:R}", value);
            }
            else if (key.StartsWith(OperatorSerialize, StringComparison.Ordinal) && value is not null)
            {
                return string.Format(_sensitiveFormatter, "{0:P}", value);
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
                    var newDictionary = new Dictionary<string, object?>(dictionary.Count);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var newKey = string.Format(_sensitiveFormatter, "{0}", entry.Key);
                        var newValue = redacted && _sensitiveFormatter.Configuration.IsSensitive(SensitiveKeyType.DictionaryEntry, newKey) ? string.Format(_sensitiveFormatter, "{0:R}", entry.Value) : SensitiveObject(entry.Value, redacted);
                        newDictionary.Add(newKey, newValue);
                    }
                    return newDictionary;
                }
                IEnumerable SensitiveEnumerable(IEnumerable enumerable, bool redacted)
                {
                    if (redacted && _sensitiveFormatter.Configuration.IsSensitive(SensitiveKeyType.CollapseArray, key))
                        return string.Format(_sensitiveFormatter, "{0:C}", enumerable);
                    var newlist = new ArrayList();
                    foreach (var value in enumerable)
                        _ = newlist.Add(SensitiveObject(value, redacted));
                    return newlist;
                }
            }
        }
    }
}

/*
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
              => left.Equals(rigth, StringComparison.Ordinal) || (left.Length > 1 && left.StartsWith(OperatorSerialize, StringComparison.Ordinal) && left[1..].Equals(rigth, StringComparison.Ordinal));
      }
      /// <summary>
      /// Returns the element at a specified index in the sequence.
      /// </summary>
      /// <param name="index">The zero-based index of the element to retrieve.</param>
      /// <param name="redacted"></param>
      /// <returns>The element at the specified position in the source sequence.</returns>
      /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in the source.</exception>
      public KeyValuePair<string, object?> GetObject(int index, bool redacted)
      {
          var kvp = _collection.ElementAt(index); // ArgumentOutOfRangeException
          var newkey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
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
          var newkey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
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
      */