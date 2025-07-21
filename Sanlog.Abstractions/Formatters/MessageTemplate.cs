using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sanlog.Formatters
{
    /// <summary>
    /// Represents a composite/named message template.
    /// </summary>
    internal sealed class MessageTemplate
    {
        /// <summary>
        /// The delimiters used by composite string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly char[] FormatDelimiters = [',', ':'];
        /// <summary>
        /// The segments that make up the composite format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly List<string> _segments = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTemplate"/> class from the specified composite/named format string.
        /// </summary>
        /// <param name="format">The composite/named format string.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="format"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in <paramref name="format"/> is invalid.</exception>
        public MessageTemplate(string format)
        {
            ArgumentNullException.ThrowIfNull(format);
            int next = 0;
            int scanIndex = 0;
            int endIndex = format.Length;
            StringBuilder stringBuilder = new(256);
            List<SegmentNamingConvention> conventions = [];
            while (scanIndex < endIndex)
            {
                int openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                {
                    _ = stringBuilder.Append(format);
                    break;
                }
                int closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);
                if (closeBraceIndex == endIndex)
                {
                    _ = stringBuilder.Append(format.AsSpan(scanIndex, endIndex - scanIndex));
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax: {index[,alignment][:formatString]}
                    int formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);
                    _ = stringBuilder.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                    // Evaluate argument name
                    string name = format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);
                    // Mixed SegmentNamingConvention is not supported
                    conventions.Add(EvaluateSegmentNaming(name));
                    if (conventions.Any(x => x == SegmentNamingConvention.AsciiDigit) && conventions.Any(x => x != SegmentNamingConvention.AsciiDigit))
                        throw new FormatException(string.Format(null, "The input string was not in the correct format. Fail to parse near offset {0}. The mixed argument names are not supported.", openBraceIndex + 1));
                    // Evaluate argument index
                    int index = conventions[^1] == SegmentNamingConvention.AsciiDigit
                        ? (int.TryParse(name, null, out int result) && int.IsPositive(result)) || result == -1
                            ? result
                            : throw new FormatException(string.Format(null, "The input string was not in the correct format. Fail to parse near offset {0}. Invalid argument index.", openBraceIndex + 1))
                        : _segments.FindIndex(x => x.Equals(name, StringComparison.Ordinal));
                    index = index == -1 ? next++ : index;
                    _ = stringBuilder.Append(index);
                    // Append lastpart
                    ReadOnlySpan<char> lastpart = format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);
                    _ = stringBuilder.Append(lastpart);
                    scanIndex = closeBraceIndex + 1;
                    // Add the next format item
                    _segments.Add(name);
                }
            }
            CompositeFormat = CompositeFormat.Parse(stringBuilder.ToString()); // FormatException

            static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
            {
                // Example: {{prefix{{{Argument}}}suffix}}.
                int braceIndex = endIndex;
                int scanIndex = startIndex;
                int braceOccurrenceCount = 0;
                while (scanIndex < endIndex)
                {
                    if (braceOccurrenceCount > 0 && format[scanIndex] != brace)
                    {
                        if (braceOccurrenceCount % 2 == 0)
                        {
                            // Even number of '{' or '}' found. Proceed search with next occurrence of '{' or '}'
                            braceOccurrenceCount = 0;
                            braceIndex = endIndex;
                        }
                        else
                        {
                            // An unescaped '{' or '}' found
                            break;
                        }
                    }
                    else if (format[scanIndex] == brace)
                    {
                        if (brace == '}')
                        {
                            if (braceOccurrenceCount == 0)
                                // For '}' pick the first occurrence
                                braceIndex = scanIndex;
                        }
                        else
                        {
                            // For '{' pick the last occurrence
                            braceIndex = scanIndex;
                        }
                        braceOccurrenceCount++;
                    }
                    scanIndex++;
                }
                return braceIndex;
            }
            static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
            {
                int findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }
            static SegmentNamingConvention EvaluateSegmentNaming(string value)
            {
                SegmentNamingConvention naming = SegmentNamingConvention.Undefined;
                foreach (char symbol in value)
                    naming |= char.IsAsciiDigit(symbol) ? SegmentNamingConvention.AsciiDigit : SegmentNamingConvention.OtherSymbols;
                return naming;
            }
        }

        /// <summary>
        /// Gets the composite format string.
        /// </summary>
        public CompositeFormat CompositeFormat { get; }
        /// <summary>
        /// Gets the segments that make up the composite format string.
        /// </summary>
        public IReadOnlyList<string> Segments => _segments;

        /// <summary>
        /// Replaces a format item or items in the current instance with the string representation of the corresponding objects in the specified format.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <param name="args">An array of objects to format.</param>
        /// <returns>A formatted string.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public string Format(IFormatProvider? formatProvider, params object?[] args) => string.Format(formatProvider, CompositeFormat, args);

        #region Override: Object
        /// <inheritdoc/>
        public override string ToString() => CompositeFormat.Format;
        #endregion

        /// <summary>
        /// Defines segment naming convention.
        /// </summary>
        [Flags]
        private enum SegmentNamingConvention
        {
            /// <summary>
            /// Unknown convention.
            /// </summary>
            Undefined = 0,
            /// <summary>
            /// Using ASCII digits.
            /// </summary>
            AsciiDigit = 1,
            /// <summary>
            /// Using symbols.
            /// </summary>
            OtherSymbols = 2,
            /// <summary>
            /// Mixed naming convention.
            /// </summary>
            Mixed = AsciiDigit | OtherSymbols
        }
    }
}