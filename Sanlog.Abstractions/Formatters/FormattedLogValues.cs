using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Compliance.Classification;
using System.Reflection;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Represents the wrapper of the Microsoft.Extensions.Logging.FormattedLogValues object.
    /// </summary>
    internal sealed class FormattedLogValues : IReadOnlyList<KeyValuePair<string, object?>>
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
        /// <exception cref="FormatException">A format item in <paramref name="format"/> is invalid.</exception>
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
        /// Determines whether the specified keys are equivalent.
        /// </summary>
        /// <param name="left">The first key.</param>
        /// <param name="right">The second key.</param>
        /// <returns><see langword="true"/> if the keys are considered equivalent; otherwise, <see langword="false"/>.</returns>
        private static bool IsKeyEquivalent(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            // Parameter == Parameter
            => left.Equals(right, StringComparison.Ordinal)
            // @Parameter == Parameter
            || (left.Length > 1 && left.StartsWith(OperatorSerialize, StringComparison.Ordinal) && left[1..].Equals(right, StringComparison.Ordinal))
            // [LoggerMessageAttribute] @Parameter -> parameter == Parameter
            || (left.Length > 1 && left.StartsWith(OperatorSerialize, StringComparison.OrdinalIgnoreCase) && left[1..].Equals(right, StringComparison.OrdinalIgnoreCase))
            // Parameter == @Parameter
            || (right.Length > 1 && right.StartsWith(OperatorSerialize, StringComparison.Ordinal) && right[1..].Equals(left, StringComparison.Ordinal))
            // [LoggerMessageAttribute] Parameter == @Parameter -> parameter
            || (right.Length > 1 && right.StartsWith(OperatorSerialize, StringComparison.OrdinalIgnoreCase) && right[1..].Equals(left, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// The values formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly FormattedLogValuesFormatter _formatProvider;
        /// <summary>
        /// The key-value pair collection to format.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IReadOnlyCollection<KeyValuePair<string, object?>> _collection;
        /// <summary>
        /// The composite/named format template.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MessageTemplate? _template;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified formatter and key-value pair collection to format.
        /// </summary>
        /// <param name="formatProvider">The values formatter.</param>
        /// <param name="collection">The key-value pair collection to format.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format string is invalid.</exception>
        public FormattedLogValues(FormattedLogValuesFormatter formatProvider, IReadOnlyCollection<KeyValuePair<string, object?>> collection)
        {
            _formatProvider = formatProvider ?? throw new ArgumentNullException(nameof(formatProvider));
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            var format = _collection
                .SingleOrDefault(x => x.Key.Equals(OriginalFormat, StringComparison.Ordinal))
                .Value as string;
            _ = TryGetOrAdd(format, out _template);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified formatter and composite/named format string.
        /// </summary>
        /// <param name="formatProvider">The values formatter.</param>
        /// <param name="format">The composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentException">Passed less than the minimum number of arguments that must be passed to a formatting operation.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="formatProvider"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format string is invalid.</exception>
        public FormattedLogValues(FormattedLogValuesFormatter formatProvider, string? format, params object?[] args)
            : this(formatProvider, ParseCompositeArgs(format, args)) { } // ArgumentException + ArgumentNullException + FormatException

        /// <inheritdoc/>
        public KeyValuePair<string, object?> this[int index] => GetObject(index, true);
        /// <inheritdoc/>
        public int Count => _collection.Count;
        /// <summary>
        /// Indicates whether the original format is defined.
        /// </summary>
        [MemberNotNullWhen(true, nameof(_template))]
        public bool HasOriginalFormat => _template is not null;

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (var index = 0; index < Count; ++index)
                yield return GetObject(index, true);
        }
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
            var newKey = HasOriginalFormat && _template.Segments.SingleOrDefault(segment => IsKeyEquivalent(kvp.Key, segment)) is string segment
                ? segment
                : kvp.Key;
            var newValue = ProcessValue(newKey, kvp.Value, redacted);
            return KeyValuePair.Create(newKey[(newKey.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..], newValue);
        }

        /// <summary>
        /// Projects each element processes through the formatter into a string key-value pair collection.
        /// </summary>
        /// <returns>An enumerable whose elements were processed through formatter.</returns>
        public IReadOnlyList<KeyValuePair<string, string?>> SelectToList()
        {
            const string SimpleFormat = "{0}";
            return this
                .Select(x => KeyValuePair.Create<string, string?>(x.Key, string.Format(_formatProvider, SimpleFormat, x.Value)))
                .ToList();
        }
        /// <inheritdoc/>
        public override string? ToString()
        {
            if (HasOriginalFormat)
            {
                var args = TakeBySegmentOrder(_template, _collection, (index, segment) =>
                {
                    var kvp = _collection.ElementAt(index);
                    var newValue = ProcessValue(segment, kvp.Value, true);
                    return newValue;
                });
                return _template.Format(_formatProvider, args);
            }
            else
            {
                return NullFormat;
            }

            static object?[] TakeBySegmentOrder(MessageTemplate messageTemplate, IReadOnlyCollection<KeyValuePair<string, object?>> collection, Func<int, string, object?> callback)
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
                        if (IsKeyEquivalent(collection.ElementAt(i).Key, segment))
                        {
                            index = i;
                            break;
                        }
                    }
                    dictionary[segment] = index != NotFound ? callback.Invoke(index, segment) : null; // The element maybe not found
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
        private object? ProcessValue(string key, object? value, bool redacted)
        {
            if (value is null)
            {
                return _formatProvider.Format(null, value, _formatProvider);
            }
            if (redacted)
            {
                var member = value.GetType();
                if (member.IsDefined(typeof(DataClassificationAttribute)))
                    return _formatProvider.Format(FormattedLogValuesFormatter.FormatRedacted, value, _formatProvider);
            }
            return key.StartsWith(OperatorSerialize, StringComparison.Ordinal)
                ? _formatProvider.Format(FormattedLogValuesFormatter.FormatSerialize, value, _formatProvider)
                : value;
        }
    }
}