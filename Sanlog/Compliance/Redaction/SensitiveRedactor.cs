using System;
using Microsoft.Extensions.Compliance.Redaction;

namespace Sanlog.Compliance.Redaction
{
    /// <summary>
    /// Redactor that replaces anything with <see cref="RedactedValue"/>.
    /// </summary>
    internal sealed class SensitiveRedactor : Redactor
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
            return RedactedValue.TryCopyTo(destination)
                ? RedactedValue.Length
                : throw new ArgumentException($"Buffer too small, needed a size of {RedactedValue.Length} but got {destination.Length}", nameof(destination));
        }
    }
}