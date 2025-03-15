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
    public sealed class FormattedLogValuesFormatterOptions : IReadOnlyList<KeyValuePair<Type, string?>>
    {
        /// <summary>
        /// Gets a read-only, singleton instance of <see cref="FormattedLogValuesFormatterOptions"/> that uses the default configuration.
        /// </summary>
        public static readonly FormattedLogValuesFormatterOptions Default = new FormattedLogValuesFormatterOptions(CultureInfo.InvariantCulture)
            .SetFormat<Enum>("D")
            .SetFormat<float>("G9")
            .SetFormat<double>("G17")
            .SetFormat<BigInteger>("R")
            .SetFormat<DateOnly>("O")
            .SetFormat<TimeOnly>("O")
            .SetFormat<DateTime>("O")
            .SetFormat<DateTimeOffset>("O")
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
        private readonly Dictionary<Type, string?> _formatters = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatterOptions"/> class.
        /// </summary>
        /// <param name="culture">The formatting culture.</param>
        public FormattedLogValuesFormatterOptions(CultureInfo? culture = null) => _culture = culture;
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatterOptions"/> based on the specified configuration.
        /// </summary>
        /// <param name="options">The based configuration.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        public FormattedLogValuesFormatterOptions(FormattedLogValuesFormatterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _culture = options._culture;
            _formatters = options._formatters;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of elements in source.</exception>
        public KeyValuePair<Type, string?> this[int index] => _formatters.ElementAt(index);
        /// <inheritdoc/>
        public int Count => _formatters.Count;
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
        /// Gets the format associated with the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the instance to format.</param>
        /// <returns>The format to use. -or- A null reference to use the default format defined for the type of the <see cref="IFormattable"/> implementation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> is <see langword="null"/>.</exception>
        public string? GetFormat(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return _formatters.TryGetValue(type, out var format) ? format : null;
        }
        /// <summary>
        /// Marks the current instance as read-only to prevent any further user modification.
        /// </summary>
        /// <returns>Returns the current instance.</returns>
        public FormattedLogValuesFormatterOptions MakeReadOnly()
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
        public FormattedLogValuesFormatterOptions SetFormat<T>(string? format) where T : IFormattable
        {
            CheckReadOnly(); // InvalidOperationException
            var type = typeof(T);
            if (_formatters.ContainsKey(type))
                _formatters.Add(type, format);
            else
            {
                _formatters[type] = format;
            }
            return this;
        }
        /// <summary>
        /// Throws an exception if the current instance is read-only to prevent any further user modification.
        /// </summary>
        /// <exception cref="InvalidOperationException">The current instance is read-only to prevent any further user modification.</exception>
        private void CheckReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("The current instance is read-only to prevent any further user modification.");
        }
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<Type, string?>> GetEnumerator() => _formatters.GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}