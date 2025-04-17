using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Represents the wrapper of the Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    internal sealed class FormattedLogValues : IEnumerable<KeyValuePair<string, object?>>
    {
        /// <summary>
        /// The operator in front of the argument name tells the formatter to serialize the object passed in, rather than convert it using ToString method.
        /// </summary>
        public const string OperatorSerialize = "@";
        /// <summary>
        /// The message format that represents a null format.
        /// </summary>
        public const string NullFormat = "[null]";
        /// <summary>
        /// The element key that represents a structured logging message.
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
        private static readonly ConcurrentDictionary<string, MessageTemplate> CachedMessageTemplates = [];

        /// <summary>
        /// The values formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FormattedLogValuesFormatter _formatter;
        /// <summary>
        /// The key-value pair collection to format.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyCollection<KeyValuePair<string, object?>> _collection;
        /// <summary>
        /// The composite/named format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string? _format;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified formatter and key-value pair collection to format.
        /// </summary>
        /// <param name="formatter">The values formatter.</param>
        /// <param name="collection">The key-value pair collection to format.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> or <paramref name="collection"/> is <see langword="null"/>.</exception>
        public FormattedLogValues(FormattedLogValuesFormatter formatter, IReadOnlyCollection<KeyValuePair<string, object?>> collection)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            ArgumentNullException.ThrowIfNull(collection);
            _formatter = formatter;
            _collection = collection;
            _format = _collection.SingleOrDefault(x => x.Key.Equals(OriginalFormat, StringComparison.Ordinal)).Value is string format ? format : null;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified formatter and composite/named format string.
        /// </summary>
        /// <param name="formatter">The values formatter.</param>
        /// <param name="format">The composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentException">Passed less than the minimum number of arguments that must be passed to a formatting operation.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        public FormattedLogValues(FormattedLogValuesFormatter formatter, string? format, params object?[] args)
            : this(formatter, ParseCompositeArgs(format, args)) { } // ArgumentException + ArgumentNullException

        /// <summary>
        /// Indicates whether the original format is defined.
        /// </summary>
        public bool HasOriginalFormat => _format is not null;

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _collection.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in the source.</exception>
        public KeyValuePair<string, object?> GetObject(int index, bool redacted)
        {
            var kvp = _collection.ElementAt(index); // ArgumentOutOfRangeException
            var newKey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
            var newValue = FormatSensitiveObject(kvp.Key, kvp.Value, redacted);
            return KeyValuePair.Create(newKey, newValue);
        }
        /// <summary>
        /// Gets the element with the specified key in the read-only list.
        /// </summary>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element with the specified key in the read-only list.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="key"/> does not exist in the sequence. -or- More than one element satisfies the condition. -or- The source sequence is empty.</exception>
        public KeyValuePair<string, object?> GetObject(string key, bool redacted)
        {
            ArgumentNullException.ThrowIfNull(key);
            var kvp = _collection.Single(x => EqualsSensitiveKey(x.Key, key)); // InvalidOperationException
            var newKey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
            var newValue = FormatSensitiveObject(kvp.Key, kvp.Value, redacted);
            return KeyValuePair.Create(newKey, newValue);
        }
        /// <summary>
        /// Projects each element processes through the formatter into a string key-value pair collection.
        /// </summary>
        /// <returns>An enumerable whose elements were processed through formatter.</returns>
        public IReadOnlyList<KeyValuePair<string, string?>> SelectToFormat()
        {
            const string SimpleFormat = "{0}";
            return this
                .Select(x => GetObject(x.Key, true))
                .Select(x => KeyValuePair.Create<string, string?>(x.Key, string.Format(_formatter, SimpleFormat, x.Value)))
                .ToList();
        }
        /// <inheritdoc/>
        public override string? ToString()
        {
            if (TryGetOrAdd(_format, out var messageTemplate))
            {
                var args = TakeBySegmentOrder(messageTemplate, _collection, (index) => GetObject(index, true));
                return messageTemplate.Format(_formatter, args);
            }
            else
            {
                return NullFormat;
            }

            static object?[] TakeBySegmentOrder(MessageTemplate messageTemplate, IReadOnlyCollection<KeyValuePair<string, object?>> collection, Func<int, object?> callback)
            {
                const int NotFound = -1;

                var dictionary = new Dictionary<string, object?>();
                foreach (var segment in messageTemplate.Segments)
                {
                    if (dictionary.ContainsKey(segment))
                    {
                        continue;
                    }
                    // Defines the first occurrence of the key instead of directly using GetObject(string, bool) to prevent InvalidOperationException
                    var index = NotFound;
                    for (var i = 0; i < collection.Count; ++i)
                    {
                        if (EqualsSensitiveKey(collection.ElementAt(i).Key, segment))
                        {
                            index = i;
                            break;
                        }
                    }
                    dictionary[segment] = index != NotFound ? callback.Invoke(index) : null; // The element maybe not found
                }
                return [.. dictionary.Values];
            }
        }
        /// <summary>
        /// Processes a value through formatter.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to process.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>A new value considering the concealment of confidential data.</returns>
        private object? FormatSensitiveObject(string key, object? value, bool redacted)
        {
            if (redacted)
            {
                return _formatter.Format(FormattedLogValuesFormatter.FormatRedacted, value, _formatter);
            }
            else if (key.StartsWith(OperatorSerialize, StringComparison.Ordinal) && value is not null)
            {
                return _formatter.Format(FormattedLogValuesFormatter.FormatSerialize, value, _formatter);
            }
            return value;
        }

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
        /// Creates a dictionary from an object array containing zero or more objects to format linked with a composite/named <paramref name="format"/> string.
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
                dictionary.Add(OriginalFormat, format);
            }
            return dictionary;
        }
        /// <summary>
        /// Determines whether the specified keys are considered equal.
        /// </summary>
        /// <param name="left">The first key.</param>
        /// <param name="rigth">The second key.</param>
        /// <returns><see langword="true"/> if the keys are considered equal; otherwise, <see langword="false"/>.</returns>
        private static bool EqualsSensitiveKey(ReadOnlySpan<char> left, ReadOnlySpan<char> rigth)
            => left.Equals(rigth, StringComparison.Ordinal) || (left.Length > 1 && left.StartsWith(OperatorSerialize, StringComparison.Ordinal) && left[1..].Equals(rigth, StringComparison.Ordinal));
    }
}