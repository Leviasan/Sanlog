using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Sanlog
{
    /// <summary>
    /// Represents the configuration of the <see cref="FormattedLogValuesFormatter"/>.
    /// </summary>
    public sealed class FormattedLogValuesFormatterOptions
    {
        /// <summary>
        /// The <see cref="DateTime"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _dateTimeFormat;
        /// <summary>
        /// The <see cref="DateTimeOffset"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _dateTimeOffsetFormat;
        /// <summary>
        /// The <see cref="Enum"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _enumFormat;
        /// <summary>
        /// The <see cref="float"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _singleFormat;
        /// <summary>
        /// The <see cref="double"/> format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string? _doubleFormat;

        /// <summary>
        /// Gets a value indicating whether the configuration is read-only.
        /// </summary>
        public bool IsReadOnly { get; private set; }
        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> format string.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
        public string? DateTimeFormat
        {
            get => _dateTimeFormat;
            set
            {
                CheckReadOnly();
                _dateTimeFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="DateTimeOffset"/> format string.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.DateTimeFormat)]
        public string? DateTimeOffsetFormat
        {
            get => _dateTimeOffsetFormat;
            set
            {
                CheckReadOnly();
                _dateTimeOffsetFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="Enum"/> format string.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.EnumFormat)]
        public string? EnumFormat
        {
            get => _enumFormat;
            set
            {
                CheckReadOnly();
                _enumFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="float"/> format string.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.NumericFormat)]
        public string? SingleFormat
        {
            get => _singleFormat;
            set
            {
                CheckReadOnly();
                _singleFormat = value;
            }
        }
        /// <summary>
        /// Gets or sets the <see cref="double"/> format string.
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.NumericFormat)]
        public string? DoubleFormat
        {
            get => _doubleFormat;
            set
            {
                CheckReadOnly();
                _doubleFormat = value;
            }
        }

        /// <summary>
        /// Makes the configuration read-only.
        /// </summary>
        /// <returns>Returns the current instance.</returns>
        public FormattedLogValuesFormatterOptions MakeReadOnly()
        {
            IsReadOnly = true;
            return this;
        }
        /// <summary>
        /// Throws an exception if the configuration is read-only.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        private void CheckReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("The configuration is read-only.");
        }
    }
}