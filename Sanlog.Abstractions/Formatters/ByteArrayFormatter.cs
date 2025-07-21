using System;
using System.Globalization;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Represents the formatter that supports custom formatting of <see cref="byte"/> array object.
    /// </summary>
    public sealed class ByteArrayFormatter : IValueFormatter
    {
        /// <summary>
        /// A string of hexadecimal pairs separated by hyphens, where each pair represents the corresponding element in value; for example, "7F-2C-4A-00".
        /// </summary>
        public const string FormatHex = "H";
        /// <summary>
        /// The format "[System.Byte[{Count}]]", where {Count} is the length of the byte array.
        /// </summary>
        public const string FormatRedacted = "R";

        /// <inheritdoc/>
        public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

        /// <inheritdoc/>
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
        {
            if (Equals(formatProvider) && arg is byte[] bytes)
            {
                if (string.IsNullOrEmpty(format))
                {
                    return bytes.ToString()!;
                }
                else if (format.Equals(FormatHex, StringComparison.Ordinal))
                {
                    return BitConverter.ToString(bytes);
                }
                else if (format.Equals(FormatRedacted, StringComparison.Ordinal))
                {
                    return $"[*{bytes.Length} {typeof(byte).Name}*]";
                }
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "'{0}' cannot be used to format {1}.", format, arg.GetType()));
            }
            return DefaultFallback(format, arg, Equals(formatProvider) ? null : formatProvider);

            static string DefaultFallback(string? format, object? arg, IFormatProvider? formatProvider)
            {
                return arg switch
                {
                    IFormattable formattable => formattable.ToString(format, formatProvider),
                    _ => Convert.ToString(arg, formatProvider) ?? string.Empty
                };
            };
        }
    }
}