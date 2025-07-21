using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Compliance.Classification;

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
            int index = 0;
            Dictionary<string, object?> dictionary = [];
            if (TryGetOrAdd(format, out MessageTemplate? messageTemplate)) // FormatException
            {
                if (args.Length < messageTemplate.CompositeFormat.MinimumArgumentCount)
                {
                    throw new ArgumentException("Passed less than the minimum number of arguments that must be passed to a formatting operation.", nameof(args));
                }
                foreach (string segment in messageTemplate.Segments)
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
        private static bool IsEquivalent(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            return Equivalent(left, right) || Equivalent(right, left);

            static bool Equivalent(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            {
                // Parameter == Parameter
                return left.Equals(right, StringComparison.Ordinal)
                    // @Parameter == Parameter && [LoggerMessageAttribute] @Parameter == @Parameter -> parameter
                    || (left.Length > 1 && left.StartsWith(OperatorSerialize, StringComparison.Ordinal) && left[1..].Equals(right, StringComparison.OrdinalIgnoreCase));
            }
        }

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
        /// The composite/named format template.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly MessageTemplate? _template;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified formatter and key-value pair collection to format.
        /// </summary>
        /// <param name="formatter">The values formatter.</param>
        /// <param name="collection">The key-value pair collection to format.</param>
        /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format string is invalid.</exception>
        public FormattedLogValues(FormattedLogValuesFormatter formatter, IReadOnlyCollection<KeyValuePair<string, object?>> collection)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            string? format = _collection
                .SingleOrDefault(x => x.Key.Equals(OriginalFormat, StringComparison.Ordinal))
                .Value as string;
            _ = TryGetOrAdd(format, out _template);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified formatter and composite/named format string.
        /// </summary>
        /// <param name="formatter">The values formatter.</param>
        /// <param name="format">The composite/named format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <exception cref="ArgumentException">Passed less than the minimum number of arguments that must be passed to a formatting operation.</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="formatter"/> or <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format string is invalid.</exception>
        public FormattedLogValues(FormattedLogValuesFormatter formatter, string? format, params object?[] args)
            : this(formatter, ParseCompositeArgs(format, args)) { } // ArgumentException + ArgumentNullException + FormatException

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
            for (int index = 0; index < Count; ++index)
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
            KeyValuePair<string, object?> kvp = _collection.ElementAt(index); // ArgumentOutOfRangeException
            string newKey = HasOriginalFormat && _template.Segments.SingleOrDefault(segment => IsEquivalent(kvp.Key, segment)) is string segment ? segment : kvp.Key;
            object? newValue = ProcessValue(newKey, kvp.Value, redacted);
            return KeyValuePair.Create(newKey[(newKey.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..], newValue);

            object? ProcessValue(string key, object? value, bool redacted)
            {
                if (value is null)
                {
                    return string.Format(_formatter, "{0}", value);
                }
                if (redacted)
                {
                    Type member = value.GetType();
                    if (member.IsDefined(typeof(DataClassificationAttribute)))
                        return string.Format(_formatter, "{0:R}", value);
                }
                return key.StartsWith(OperatorSerialize, StringComparison.Ordinal)
                    ? string.Format(_formatter, "{0:S}", value)
                    : value;
            }
        }
        /// <summary>
        /// Projects each element processes through the formatter into a string dictionary.
        /// </summary>
        /// <returns>An enumerable whose elements were processed through formatter.</returns>
        public Dictionary<string, string?> GroupByToDictionary()
        {
            return this
                .GroupBy(x => x.Key)
                .ToDictionary<IGrouping<string, KeyValuePair<string, object?>>, string, string?>(
                    keySelector: g => g.Key,
                    elementSelector: g => string.Join(", ", g.Select(x => string.Format(_formatter, "{0}", x.Value)))); // CS8619
        }
        /// <inheritdoc/>
        public override string? ToString()
        {
            if (HasOriginalFormat)
            {
                object?[] args = TakeBySegmentOrder(_template, _collection, (index, segment) =>
                {
                    KeyValuePair<string, object?> kvp = GetObject(index, true);
                    return kvp.Value;
                });
                return _template.Format(_formatter, args);
            }
            else
            {
                return NullFormat;
            }

            static object?[] TakeBySegmentOrder(MessageTemplate messageTemplate, IReadOnlyCollection<KeyValuePair<string, object?>> collection, Func<int, string, object?> callback)
            {
                const int NotFound = -1;

                Dictionary<string, object?> dictionary = [];
                foreach (string segment in messageTemplate.Segments)
                {
                    if (dictionary.ContainsKey(segment))
                    {
                        continue;
                    }
                    // Defines the first occurrence of the key instead of directly using GetObject(string, bool) to prevent InvalidOperationException
                    int index = NotFound;
                    for (int i = 0; i < collection.Count; ++i)
                    {
                        if (IsEquivalent(collection.ElementAt(i).Key, segment))
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
    }
}