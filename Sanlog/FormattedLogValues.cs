﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Classification;

namespace Sanlog
{
    internal static class IServiceCollectionExtensions
    {
        public static void AddCompliance(this IServiceCollection services)
        {
            _ = services.AddRedaction(x =>
            {
                _ = x.SetRedactor<ErasingRedactor>(new DataClassificationSet(SensitiveDataAttribute.DataClassification));
                _ = x.SetHmacRedactor(x =>
                {
                    x.KeyId = 1;
                    x.Key = "...";
                }, new DataClassificationSet(PIIDataAttribute.DataClassification));
            });
        }
    }
    internal sealed class SensitiveDataAttribute : DataClassificationAttribute
    {
        internal static DataClassification DataClassification { get; } = new DataClassification(nameof(SanlogLogger), nameof(SensitiveDataAttribute));

        public SensitiveDataAttribute() : base(DataClassification) { }
    }
    internal sealed class PIIDataAttribute : DataClassificationAttribute
    {
        internal static DataClassification DataClassification { get; } = new DataClassification(nameof(SanlogLogger), nameof(PIIDataAttribute));

        public PIIDataAttribute() : base(DataClassification) { }
    }
    internal sealed record Customer([SensitiveData] string Name, [PIIData] string Password);

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
        public const string NullFormat = "[null]";
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
        /// Creates a dictionary from an object array containing zero or more objects to format linked with <paramref name="format"/> a composite/named format string.
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
        /// The configuration of the formatter.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SensitiveFormatterOptions? _sensitiveConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValues"/> class with the specified key-value pair collection to format.
        /// </summary>
        /// <param name="collection">The key-value pair collection to format.</param>
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
        public FormattedLogValues(string? format, params object?[] args) : this(ParseCompositeArgs(format, args)) { } // ArgumentException + ArgumentNullException

        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }
        /// <summary>
        /// Gets or sets the configuration of the formatter.
        /// </summary>
        public SensitiveFormatterOptions SensitiveConfiguration
        {
            get => _sensitiveConfiguration ??= new SensitiveFormatterOptions();
            set => _sensitiveConfiguration = value;
        }
        /// <summary>
        /// Gets or sets the configuration of the <see cref="FormattedLogValuesFormatter"/>.
        /// </summary>
        public FormattedLogValuesFormatterOptions? FormattedConfiguration { get; set; }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in the source.</exception>
        public KeyValuePair<string, object?> this[int index] => this[index, true]; // ArgumentOutOfRangeException
        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="index"/> is less than 0 or greater than or equal to the number of elements in the source.</exception>
        public KeyValuePair<string, object?> this[int index, bool redacted]
        {
            get
            {
                var kvp = _collection.ElementAt(index); // ArgumentOutOfRangeException
                var newValue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
                var newKey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
                return KeyValuePair.Create(newKey, newValue);
            }
        }
        /// <summary>
        /// Gets the element with the specified key in the read-only list.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <returns>The element with the specified key in the read-only list.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="key"/> does not exist in the sequence. -or- More than one element satisfies the condition. -or- The source sequence is empty.</exception>
        public KeyValuePair<string, object?> this[string key] => this[key, true]; // ArgumentNullException + InvalidOperationException
        /// <summary>
        /// Gets the element with the specified key in the read-only list.
        /// </summary>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <param name="redacted">Indicates whether need to redact sensitive data.</param>
        /// <returns>The element with the specified key in the read-only list.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="key"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="key"/> does not exist in the sequence. -or- More than one element satisfies the condition. -or- The source sequence is empty.</exception>
        public KeyValuePair<string, object?> this[string key, bool redacted]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(key);
                var kvp = _collection.Single(x => x.Key == key); // InvalidOperationException
                var newValue = ProcessSensitiveObject(kvp.Key, kvp.Value, redacted);
                var newKey = kvp.Key[(kvp.Key.StartsWith(OperatorSerialize, StringComparison.Ordinal) ? 1 : 0)..];
                return KeyValuePair.Create(newKey, newValue);
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
            if (TryGetOrAdd(_format, out var messageTemplate))
            {
                var args = TakeBySegmentOrder(messageTemplate, _collection, (index) => this[index].Value);
                var formatter = new FormattedLogValuesFormatter { CultureInfo = CultureInfo, Configuration = FormattedConfiguration };
                return messageTemplate.Format(formatter, args);
            }
            else
            {
                return NullFormat;
            }

            static object?[] TakeBySegmentOrder(MessageTemplate messageTemplate, IReadOnlyCollection<KeyValuePair<string, object?>> collection, Func<int, object?> indexer)
            {
                var dictionary = new Dictionary<string, object?>();
                foreach (var segment in messageTemplate.Segments)
                {
                    if (dictionary.ContainsKey(segment))
                    {
                        continue;
                    }
                    // Defines the first occurrence of the key instead of directly using this[string, bool] to prevent InvalidOperationException
                    var index = -1;
                    for (var i = 0; i < collection.Count; ++i)
                    {
                        if (EqualsOrdinalString(collection.ElementAt(i).Key, segment))
                        {
                            index = i;
                        }
                    }
                    dictionary[segment] = index != -1 ? indexer.Invoke(index) : null; // The element maybe not found
                }
                return [.. dictionary.Values];

                static bool EqualsOrdinalString(ReadOnlySpan<char> left, ReadOnlySpan<char> rigth)
                    => left.Equals(rigth, StringComparison.Ordinal) || (left.Length > 1 && left.StartsWith(OperatorSerialize, StringComparison.Ordinal) && left[1..].Equals(rigth, StringComparison.Ordinal));
            }
        }
        /// <summary>
        /// Projects each element processes through formatters into a string key-value pair collection.
        /// </summary>
        /// <returns>An enumerable whose elements were processed through formatters.</returns>
        public IEnumerable<KeyValuePair<string, string?>> FormatToList()
        {
            var formatter = new FormattedLogValuesFormatter { CultureInfo = CultureInfo, Configuration = FormattedConfiguration };
            return this.Select(x => KeyValuePair.Create<string, string?>(x.Key, string.Format(formatter, "{0}", x.Value))); // 'this.Select' see 'GetEnumerator'
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
            var formatter = new FormattedLogValuesFormatter { CultureInfo = CultureInfo, Configuration = FormattedConfiguration };
            if (redacted && SensitiveConfiguration.IsSensitive(SensitiveKeyType.SegmentName, key))
            {
                return string.Format(formatter, "{0:R}", value);
            }
            else if (key.StartsWith(OperatorSerialize, StringComparison.Ordinal) && value is not null)
            {
                return string.Format(formatter, "{0:S}", value);
            }
            else if (redacted && SensitiveConfiguration.IsSensitive(SensitiveKeyType.CollapseArray, key))
            {
                return string.Format(formatter, "{0:C}", value);
            }
            return SensitiveObject(key, value, redacted, SensitiveConfiguration, formatter);

            static object? SensitiveObject(string? key, object? value, bool redacted, SensitiveFormatterOptions configuration, IFormatProvider provider)
            {
                return value switch
                {
                    string str => str, // string implements IEnumerable so must be process before
                    IDictionary dictionary => SensitiveDictionary(dictionary, redacted, configuration, provider), // IDictionary implements IEnumerable so must be process before
                    IEnumerable enumerable => SensitiveEnumerable(key, enumerable, redacted, configuration, provider),
                    _ => value
                };
                static IDictionary SensitiveDictionary(IDictionary dictionary, bool redacted, SensitiveFormatterOptions configuration, IFormatProvider provider)
                {
                    var newDictionary = new Dictionary<string, object?>(dictionary.Count);
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var newKey = string.Format(provider, "{0}", entry.Key);
                        var newValue = redacted && configuration.IsSensitive(SensitiveKeyType.DictionaryEntry, newKey) ? string.Format(provider, "{0:R}", entry.Value) : SensitiveObject(newKey, entry.Value, redacted, configuration, provider);
                        newDictionary.Add(newKey, newValue);
                    }
                    return newDictionary;
                }
                static IEnumerable SensitiveEnumerable(string? key, IEnumerable enumerable, bool redacted, SensitiveFormatterOptions configuration, IFormatProvider provider)
                {
                    if (redacted && key is not null && configuration.IsSensitive(SensitiveKeyType.CollapseArray, key))
                        return string.Format(provider, "{0:C}", enumerable);
                    var newlist = new ArrayList();
                    foreach (var value in enumerable)
                        _ = newlist.Add(SensitiveObject(null, value, redacted, configuration, provider));
                    return newlist;
                }
            }
        }
    }
}