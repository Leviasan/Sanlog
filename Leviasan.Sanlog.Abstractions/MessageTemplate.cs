using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a message template.
    /// </summary>
    public sealed class MessageTemplate
    {
        /// <summary>
        /// The delimiters used by composite string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly char[] FormatDelimiters = [',', ':'];

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTemplate"/> class from the specified message template.
        /// </summary>
        /// <param name="value">The message template to parse.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        public MessageTemplate(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var next = 0;
            var scanIndex = 0;
            var endIndex = value.Length;
            var stringBuilder = new StringBuilder(256);
            var namedFormatStringItems = new List<string>();
            var conventions = new List<SegmentNamingConvention>();
            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(value, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                {
                    stringBuilder.Append(value);
                    break;
                }
                var closeBraceIndex = FindBraceIndex(value, '}', openBraceIndex, endIndex);
                if (closeBraceIndex == endIndex)
                {
                    _ = stringBuilder.Append(value.AsSpan(scanIndex, endIndex - scanIndex));
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax: {index[,alignment][:formatString]}
                    var formatDelimiterIndex = FindIndexOfAny(value, FormatDelimiters, openBraceIndex, closeBraceIndex);
                    _ = stringBuilder.Append(value.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                    // Evaluate argument name
                    var name = value.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);
                    // Mixed argument name is not supported
                    conventions.Add(EvaluateSegmentNaming(name));
                    if (conventions.Any(x => x == SegmentNamingConvention.AsciiDigit) && conventions.Any(x => x != SegmentNamingConvention.AsciiDigit))
                        throw new FormatException(string.Format(null, "The input string was not in the correct format. Fail to parse near offset {0}. The mixed argument names are not supported.", openBraceIndex + 1));
                    // Evaluate argument index
                    var index = conventions[^1] == SegmentNamingConvention.AsciiDigit
                        ? int.TryParse(name, null, out var result) && int.IsPositive(result) || result == -1
                            ? result
                            : throw new FormatException(string.Format(null, "The input string was not in the correct format. Fail to parse near offset {0}. Invalid argument index.", openBraceIndex + 1))
                        : namedFormatStringItems.FindIndex(x => x.Equals(name, StringComparison.Ordinal));
                    index = index == -1 ? next++ : index;
                    _ = stringBuilder.Append(index);
                    // Append lastpart
                    var lastpart = value.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);
                    _ = stringBuilder.Append(lastpart);
                    scanIndex = closeBraceIndex + 1;
                    // Add the next format item
                    namedFormatStringItems.Add(name);
                }
            }
            CompositeFormat = CompositeFormat.Parse(stringBuilder.ToString()); // FormatException
            Segments = [.. namedFormatStringItems];

            // Summary: Reports the zero-based index of the first occurrence in string instance of brace in a specified array of Unicode characters.
            // The search starts at a specified character position and examines a specified number of character positions.
            // Param (format): The search string.
            // Param (brace): The symbol to search.
            // Param (startIndex): The search starting position.
            // Param (endIndex): The search finish position.
            // Returns: The zero-based index position of value if that character is found, or endIndex if it is not.
            static int FindBraceIndex(string format, char brace, int startIndex, int endIndex)
            {
                // Example: {{prefix{{{Argument}}}suffix}}.
                var braceIndex = endIndex;
                var scanIndex = startIndex;
                var braceOccurrenceCount = 0;
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
            // Summary: Reports the zero-based index of the first occurrence in string instance of any character in a specified array of Unicode characters.
            // The search starts at a specified character position and examines a specified number of character positions.
            // Param (format): The search string.
            // Param (chars): A Unicode character array containing one or more characters to seek.
            // Param (startIndex): The search starting position.
            // Param (endIndex): The search finish position.
            // Returns: The zero-based index position of the first occurrence in this instance where any character in anyOf was found; endIndex if no character in anyOf was found.
            static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
            {
                var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }
            // Summary: Evaluates a segment naming convention.
            // Param (value): The string value to evaluate.
            // Returns: AsciiDigit if all symbols in the specified string are categorized as an ASCII digit; OtherSymbols if any symbols in the specified string are not categorized as an ASCII digit; otherwise Undefined.
            static SegmentNamingConvention EvaluateSegmentNaming(string value)
            {
                var naming = SegmentNamingConvention.Undefined;
                foreach (var symbol in value)
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
        public IReadOnlyList<string> Segments { get; }

        /// <summary>
        /// Replaces a format item or items in the current instance with the string representation of the corresponding objects in the specified format.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <param name="args">An array of objects to format.</param>
        /// <returns>A formatted string.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public string Format(IFormatProvider? formatProvider, params object?[] args) => string.Format(formatProvider, CompositeFormat, args);
        /// <inheritdoc/>
        public override string OriginalFormatBuild() => CompositeFormat.Format;

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