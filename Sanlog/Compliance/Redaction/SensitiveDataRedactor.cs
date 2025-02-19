using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Redaction;

namespace Sanlog.Compliance.Redaction
{
    /// <summary>
    /// Redactor that replaces anything with <see cref="RedactedValue"/>.
    /// </summary>
    public sealed class SensitiveDataRedactor : Redactor
    {
        /// <summary>
        /// The redacted value.
        /// </summary>
        public const string RedactedValue = "[Redacted]";

        /// <inheritdoc/>
        public override int GetRedactedLength(ReadOnlySpan<char> input) => RedactedValue.Length;
        /// <inheritdoc/>
        public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (!RedactedValue.TryCopyTo(destination))
                ThrowArgumentException($"Buffer too small, needed a size of {RedactedValue.Length} but got {destination.Length}", nameof(destination));
            return RedactedValue.Length;
        }
        /// <summary>
        /// Throws an <see cref="ArgumentException"/> with a specified error message and the name of the parameter that causes this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <exception cref="ArgumentException">The method will never return under any circumstance.</exception>
        [DoesNotReturn]
        private static void ThrowArgumentException(string? message, string? paramName) => throw new ArgumentException(message, paramName);
    }
}