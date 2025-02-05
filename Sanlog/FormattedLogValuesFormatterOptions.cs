﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Sanlog
{
    /// <summary>
    /// Represents the configuration of the <see cref="FormattedLogValuesFormatter"/>.
    /// </summary>
    public sealed class FormattedLogValuesFormatterOptions
    {
        /// <summary>
        /// Gets a read-only, singleton instance of <see cref="FormattedLogValuesFormatterOptions"/> that uses the default configuration.
        /// </summary>
        public static readonly FormattedLogValuesFormatterOptions Default = new FormattedLogValuesFormatterOptions()
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
        /// The overridden format for specified types.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, string?> _formatters = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatterOptions"/> with default configuration.
        /// </summary>
        public FormattedLogValuesFormatterOptions() : this(Default) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FormattedLogValuesFormatterOptions"/> based on the specified configuration.
        /// </summary>
        /// <param name="options">The based configuration.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        public FormattedLogValuesFormatterOptions(FormattedLogValuesFormatterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _formatters = options._formatters;
        }

        /// <summary>
        /// Gets a value indicating whether the current instance has been locked for user modification.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Gets the overriden format associated with the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public string? GetFormat<T>() => _formatters.TryGetValue(typeof(T), out var format) ? format : null;
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
            CheckReadOnly();
            var type = typeof(T);
            if (_formatters.ContainsKey(type))
            {
                _formatters.Add(type, format);
            }
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
                ThrowInvalidOperationException("The current instance is read-only to prevent any further user modification.");
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <exception cref="InvalidOperationException">The method will never return under any circumstance.</exception>
        [DoesNotReturn]
        private static void ThrowInvalidOperationException(string? message) => throw new InvalidOperationException(message);
    }
}