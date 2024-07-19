using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a message template.
    /// </summary>
    public sealed class MessageTemplate : IReadOnlyList<MessageTemplate.Segment>
    {
        /// <summary>
        /// The delimiters used by composite string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly char[] FormatDelimiters = [',', ':'];

        /// <summary>
        /// Parses the specified string as a message template.
        /// </summary>
        /// <param name="value">The message template to parse.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A message template as a composite string and information about its segments.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in template is invalid.</exception>
        public static MessageTemplate Parse(string value, IFormatProvider? formatProvider)
        {
            ArgumentNullException.ThrowIfNull(value);
            try
            {
                var itemIndex = 0;
                var scanIndex = 0;
                var endIndex = value.Length;
                var stringBuilder = new StringBuilder(256);
                var namedFormatStringItems = new List<Segment>();
                CompositeFormat compositeFormat;
                var conventions = new List<SegmentNamingConvention>();
                while (scanIndex < endIndex)
                {
                    var openBraceIndex = FindBraceIndex(value, '{', scanIndex, endIndex);
                    if (scanIndex == 0 && openBraceIndex == endIndex)
                    {
                        compositeFormat = CompositeFormat.Parse(value); // FormatException
                        return new MessageTemplate(compositeFormat, [.. namedFormatStringItems]);
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
                            throw new FormatException(string.Format(formatProvider, "The input string was not in the correct format. Fail to parse near offset {0}. The mixed argument names are not supported.", openBraceIndex + 1));
                        // Evaluate argument index
                        var index = conventions[^1] == SegmentNamingConvention.AsciiDigit ? int.Parse(name, formatProvider)
                            : namedFormatStringItems.FindIndex(x => x.Name.Equals(name, StringComparison.Ordinal)); // OverflowException
                        if (index < -1)
                            throw new OverflowException("An argument index represents a number less than 0 or greater than 2147483647.");
                        index = index == -1 ? itemIndex++ : index;
                        _ = stringBuilder.Append(index);
                        // Evaluate argument alignment
                        var lastpart = value.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);
                        _ = stringBuilder.Append(lastpart);
                        scanIndex = closeBraceIndex + 1;
                        var alignmentIndex = lastpart.IndexOf(',');
                        var formatStringIndex = lastpart.IndexOf(':');
                        var alignment = alignmentIndex == -1 ? default
                            : int.Parse(lastpart[(alignmentIndex + 1)..(formatStringIndex == -1 ? ^1 : formatStringIndex)], formatProvider); // OverflowException
                        var format = formatStringIndex == -1 ? null : lastpart[(formatStringIndex + 1)..^1].ToString();
                        // Add the next format item
                        namedFormatStringItems.Add(new Segment(name, index, alignment, format));
                    }
                }
                compositeFormat = CompositeFormat.Parse(stringBuilder.ToString()); // FormatException
                return new MessageTemplate(compositeFormat, [.. namedFormatStringItems]);
            }
            catch (OverflowException overflow)
            {
                throw new FormatException(null, overflow);
            }

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
        /// The composite format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CompositeFormat _compositeFormat;
        /// <summary>
        /// The segments that make up the composite format string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Segment[] _segments;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageTemplate"/> class with the specified parsed composite format string and information about its segments.
        /// </summary>
        /// <param name="compositeFormat">The parsed composite format string.</param>
        /// <param name="segments">The segments that make up the composite format string.</param>
        private MessageTemplate(CompositeFormat compositeFormat, Segment[] segments)
        {
            Debug.Assert(compositeFormat is not null);
            Debug.Assert(segments is not null);
            _compositeFormat = compositeFormat;
            _segments = segments;
        }

        /// <inheritdoc/>
        public Segment this[int index] => _segments[index];
        /// <inheritdoc/>
        public int Count => _segments.Length;
        /// <summary>
        /// Gets the minimum number of arguments that must be passed to a formatting operation using this instance.
        /// </summary>
        public int MinimumArgumentCount => _compositeFormat.MinimumArgumentCount;

        /// <summary>
        /// Replaces a format item or items in the current instance with the string representation of the corresponding objects in the specified format.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <param name="args">An array of objects to format.</param>
        /// <returns>A formatted string.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public string Format(IFormatProvider? formatProvider, params object?[] args) => string.Format(formatProvider, _compositeFormat, args);
        /// <inheritdoc/>
        public IEnumerator<Segment> GetEnumerator() => ((IEnumerable<Segment>)_segments).GetEnumerator();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _segments.GetEnumerator();
        /// <inheritdoc/>
        public override string ToString() => _compositeFormat.Format;

        /// <summary>
        /// Represents a segment that makes up the composite format string.
        /// </summary>
        /// <param name="Name">The name of the segment.</param>
        /// <param name="Index">The index of the segment.</param>
        /// <param name="Alignment">The alignment of the segment.</param>
        /// <param name="FormatString">The format of the segment.</param>
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Internal usage")]
        public sealed record class Segment(string Name, int Index, int Alignment, string? FormatString)
        {
            /// <summary>
            /// Replaces the item in the current instance with the string representation of the corresponding object in the specified format.
            /// </summary>
            /// <param name="provider">An object that supplies culture-specific formatting information.</param>
            /// <param name="arg0">The object to format.</param>
            /// <returns>A formatted string representation of the specified object.</returns>
            public string Format(IFormatProvider? provider, object? arg0) => string.Format(provider, $"{{0,{Alignment}:{FormatString}}}", arg0);
        }
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