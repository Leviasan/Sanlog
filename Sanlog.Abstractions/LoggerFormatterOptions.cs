using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Sanlog.Formatters;

namespace Sanlog
{
    /// <summary>
    /// Represents the configuration of the <see cref="FormattedLogValuesFormatter"/>.
    /// </summary>
    public sealed class LoggerFormatterOptions : IReadOnlyList<KeyValuePair<Type, string?>>
    {
        /// <summary>
        /// Gets a read-only, singleton instance of <see cref="LoggerFormatterOptions"/> that uses the default configuration.
        /// </summary>
        public static readonly LoggerFormatterOptions Default = new LoggerFormatterOptions(CultureInfo.InvariantCulture)
            .OverrideFormat<Enum>("D")
            .OverrideFormat<float>("G9")
            .OverrideFormat<double>("G17")
            .OverrideFormat<BigInteger>("R")
            .OverrideFormat<DateOnly>("O")
            .OverrideFormat<TimeOnly>("O")
            .OverrideFormat<DateTime>("O")
            .OverrideFormat<DateTimeOffset>("O")
            .RegisterFormatter<byte[]>(ByteArrayFormatter.Instance, ByteArrayFormatter.FormatRedacted)
            .MakeReadOnly();

        /// <summary>
        /// The formatting culture.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CultureInfo? _culture;
        /// <summary>
        /// The overridden format for specified types.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, string?> _formats = [];
        /// <summary>
        /// The formatters for specified types.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, (IValueFormatter Formatter, string? Format)> _formatters = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerFormatterOptions"/> class.
        /// </summary>
        /// <param name="culture">The formatting culture.</param>
        public LoggerFormatterOptions(CultureInfo? culture = null) => _culture = culture;
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerFormatterOptions"/> based on the specified configuration.
        /// </summary>
        /// <param name="options">The based configuration.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        public LoggerFormatterOptions(LoggerFormatterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _culture = options._culture;
            _formats = options._formats;
            _formatters = options._formatters;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of elements in source.</exception>
        public KeyValuePair<Type, string?> this[int index] => _formats.ElementAt(index);
        /// <inheritdoc/>
        public int Count => _formats.Count;
        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        /// <exception cref="InvalidOperationException">The current instance is read-only to prevent any further user modification.</exception>
        public CultureInfo? CultureInfo
        {
            get => _culture;
            set
            {
                CheckReadOnly(); // InvalidOperationException
                _culture = value;
            }
        }
        /// <summary>
        /// Gets a value indicating whether the current instance has been locked for user modification.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Throws an exception if the current instance is read-only to prevent any further user modification.
        /// </summary>
        /// <exception cref="InvalidOperationException">The current instance is read-only to prevent any further user modification.</exception>
        private void CheckReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("The current instance is read-only to prevent any further user modification.");
        }
        /// <summary>
        /// Gets the formatter associated with the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the instance to format.</param>
        /// <returns>The formatter to use; otherwise <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> is <see langword="null"/>.</exception>
        public Func<object?, string?>? GetFormatter(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return _formats.TryGetValue(type, out string? format)
                ? ((obj) => obj is IFormattable formattable ? formattable.ToString(format, _culture) : null)
                : _formatters.TryGetValue(type, out (IValueFormatter Formatter, string? Format) tuple)
                ? ((obj) => tuple.Formatter.Format(tuple.Format, obj, tuple.Formatter))
                : ((obj) => null);
        }
        /// <summary>
        /// Marks the current instance as read-only to prevent any further user modification.
        /// </summary>
        /// <returns>Returns the current instance.</returns>
        public LoggerFormatterOptions MakeReadOnly()
        {
            IsReadOnly = true;
            return this;
        }
        /// <summary>
        /// Overrides format to use for the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the instance to format.</typeparam>
        /// <param name="format">The format to use. -or- A null reference to use the default format defined for the type of the <see cref="IFormattable"/> implementation.</param>
        /// <exception cref="InvalidOperationException">The current instance is read-only to prevent any further user modification.</exception>
        /// <returns>Returns the current instance.</returns>
        public LoggerFormatterOptions OverrideFormat<T>(string? format) where T : IFormattable
        {
            CheckReadOnly(); // InvalidOperationException
            if (!_formats.TryAdd(typeof(T), format))
                _formats[typeof(T)] = format;
            return this;
        }
        /// <summary>
        /// Registers a formatter to use for the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the instance to format.</typeparam>
        /// <param name="formatter">The used formatter.</param>
        /// <param name="format">The default format if one is not specified.</param>
        /// <exception cref="InvalidOperationException">The current instance is read-only to prevent any further user modification.</exception>
        /// <returns>Returns the current instance.</returns>
        public LoggerFormatterOptions RegisterFormatter<T>(IValueFormatter formatter, string? format)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            CheckReadOnly(); // InvalidOperationException
            (IValueFormatter, string?) tuple = new(formatter, format);
            if (!_formatters.TryAdd(typeof(T), tuple))
                _formatters[typeof(T)] = tuple;
            return this;
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<Type, string?>> GetEnumerator() => _formats.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}