using System;
using System.Globalization;

namespace Sanlog
{
    /// <summary>
    /// Provides a mechanism for formatting the value of an object.
    /// </summary>
    public abstract class CustomFormatter : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// Gets or sets the formatting culture.
        /// </summary>
        public CultureInfo? CultureInfo { get; set; }

        /// <inheritdoc/>
        public virtual string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            var provider = Equals(formatProvider) ? CultureInfo : formatProvider;
            return arg switch
            {
                IFormattable formattable => formattable.ToString(format, provider),
                _ => Convert.ToString(arg, provider) ?? string.Empty
            };
        }
        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : CultureInfo?.GetFormat(formatType);
    }
}