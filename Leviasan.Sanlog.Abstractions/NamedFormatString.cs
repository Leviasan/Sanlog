using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents a parsed named format string.
    /// </summary>
    public sealed class NamedFormatString
    {
        /// <summary>
        /// The delimiters used by composite string.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly char[] FormatDelimiters = [',', ':'];

        /// <summary>
        /// Parses the specified named format string.
        /// </summary>
        /// <param name="format">The named format string to parse.</param>
        /// <returns>A composite string and information about segments in the expression.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="format"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">A format item in format is invalid.</exception>
        public static NamedFormatString Parse(string format)
        {
            ArgumentNullException.ThrowIfNull(format);
            var itemIndex = 0;
            var scanIndex = 0;
            var endIndex = format.Length;
            var stringBuilder = new StringBuilder(256);
            var namedFormatStringItems = new List<FormatSegment>();
            CompositeFormat compositeFormat;
            var naming = SegmentNamingConvention.None;
            while (scanIndex < endIndex)
            {
                var openBraceIndex = FindBraceIndex(format, '{', scanIndex, endIndex);
                if (scanIndex == 0 && openBraceIndex == endIndex)
                {
                    compositeFormat = CompositeFormat.Parse(format); // FormatException
                    return new NamedFormatString(compositeFormat, [.. namedFormatStringItems]);
                }
                var closeBraceIndex = FindBraceIndex(format, '}', openBraceIndex, endIndex);
                if (closeBraceIndex == endIndex)
                {
                    _ = stringBuilder.Append(format.AsSpan(scanIndex, endIndex - scanIndex));
                    scanIndex = endIndex;
                }
                else
                {
                    // Format item syntax: {index[,alignment][:formatString]}
                    var formatDelimiterIndex = FindIndexOfAny(format, FormatDelimiters, openBraceIndex, closeBraceIndex);
                    _ = stringBuilder.Append(format.AsSpan(scanIndex, openBraceIndex - scanIndex + 1));
                    // Substring argument name
                    var argName = format.Substring(openBraceIndex + 1, formatDelimiterIndex - openBraceIndex - 1);
                    // Mixed segment name is not allowed
                    naming |= EvaluateSegmentNaming(argName);
                    if (naming == SegmentNamingConvention.Mixed)
                        throw new FormatException($"Input string was not in a correct format. Failure to parse near offset {openBraceIndex + 1}. Expected an ASCII digit.");
                    // Evaluate argument index
                    var argIndex = naming == SegmentNamingConvention.AsciiDigit ? int.Parse(argName, CultureInfo.InvariantCulture) : namedFormatStringItems.FindIndex(x => x.Name.Equals(argName, StringComparison.Ordinal));
                    argIndex = argIndex == -1 ? itemIndex++ : argIndex;
                    _ = stringBuilder.Append(argIndex);
                    var lastpart = format.AsSpan(formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);
                    _ = stringBuilder.Append(lastpart);
                    scanIndex = closeBraceIndex + 1;
                    // Extract alignment and format
                    var alignmentIndex = lastpart.IndexOf(',');
                    var formatStringIndex = lastpart.IndexOf(':');
                    var argAlignment = int.TryParse(lastpart[(alignmentIndex + 1)..(formatStringIndex == -1 ? ^1 : formatStringIndex)], out var result) ? result : default;
                    var argFormat = formatStringIndex == -1 ? null : lastpart[(formatStringIndex + 1)..^1].ToString();
                    // Register a format item
                    namedFormatStringItems.Add(new FormatSegment(argName, argIndex, argAlignment, argFormat));
                }
            }
            compositeFormat = CompositeFormat.Parse(stringBuilder.ToString()); // FormatException
            return new NamedFormatString(compositeFormat, [.. namedFormatStringItems]);

            // Summary: Reports the zero-based index of the first occurrence in string instance of brace in a specified array of Unicode characters.
            // The search starts at a specified character position and examines a specified number of character positions.
            // Param (format): The named format string to parse.
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
            // Param (format): The named format string to parse.
            // Param (chars): A Unicode character array containing one or more characters to seek.
            // Param (startIndex): The search starting position.
            // Param (endIndex): The search finish position.
            // Returns: The zero-based index position of the first occurrence in this instance where any character in anyOf was found; endIndex if no character in anyOf was found.
            static int FindIndexOfAny(string format, char[] chars, int startIndex, int endIndex)
            {
                var findIndex = format.IndexOfAny(chars, startIndex, endIndex - startIndex);
                return findIndex == -1 ? endIndex : findIndex;
            }
            // Summary: Indicates whether all symbols in the specified string are categorized as an ASCII digit.
            // Param (value): The string to evaluate.
            // Returns: AsciiDigit if all symbols in the specified string are categorized as an ASCII digit; otherwise, Named.
            static SegmentNamingConvention EvaluateSegmentNaming(string value)
            {
                foreach (var symbol in value)
                    if (!char.IsAsciiDigit(symbol))
                        return SegmentNamingConvention.Named;
                return SegmentNamingConvention.AsciiDigit;
            }
        }
        /// <summary>
        /// Parses the specified composite format string.
        /// </summary>
        /// <param name="compositeFormat">The composite format string to parse.</param>
        /// <returns>A composite string and information about segments in the expression.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="compositeFormat"/> is <see langword="null"/>.</exception>
        public static NamedFormatString Parse(CompositeFormat compositeFormat)
        {
            ArgumentNullException.ThrowIfNull(compositeFormat);
            return Parse(compositeFormat.Format);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedFormatString"/> class with the specified parsed composite format string and information about its segments.
        /// </summary>
        /// <param name="compositeFormat">The parsed composite format string.</param>
        /// <param name="segments">The parsed segments that make up the composite format string.</param>
        private NamedFormatString(CompositeFormat compositeFormat, FormatSegment[] segments)
        {
            Debug.Assert(compositeFormat is not null);
            Debug.Assert(segments is not null);
            CompositeFormat = compositeFormat;
            Segments = segments;
        }

        /// <summary>
        /// Gets the parsed composite format string.
        /// </summary>
        public CompositeFormat CompositeFormat { get; }
        /// <summary>
        /// Gets the parsed segments that make up the composite format string.
        /// </summary>
        public IReadOnlyList<FormatSegment> Segments { get; }

        /// <summary>
        /// Replaces the format item or items in the current instance with the string representation of the corresponding objects in the specified format.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <param name="args">An array of objects to format.</param>
        /// <returns>The formatted string.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">The index of a format item is greater than or equal to the number of supplied arguments.</exception>
        public string Format(IFormatProvider? formatProvider, params object?[] args) => string.Format(formatProvider, CompositeFormat, args);

        /// <summary>
        /// Represents a segment that makes up the composite format string.
        /// </summary>
        /// <param name="Name">The name of the segment.</param>
        /// <param name="Index">The index of the segment.</param>
        /// <param name="Alignment">The alignment of the segment.</param>
        /// <param name="FormatString">The format of the segment.</param>
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Used only in NamedCompositeString context")]
        public sealed record class FormatSegment(string Name, int Index, int Alignment, string? FormatString)
        {
            /// <summary>
            /// Replaces the format item with a string representation of the specified object using alignment and format string of the current instance.
            /// </summary>
            /// <param name="provider">An object that supplies culture-specific formatting information.</param>
            /// <param name="arg0">The object to format.</param>
            /// <returns>A formatted string representation of the specified object.</returns>   
            public string Format(IFormatProvider? provider, object? arg0)
            {
                var stringBuilder = new StringBuilder(256).Append("{0");
                if (Alignment != 0)
                    _ = stringBuilder.Append(CultureInfo.InvariantCulture, $",{Alignment}");
                if (!string.IsNullOrEmpty(FormatString))
                    _ = stringBuilder.Append(CultureInfo.InvariantCulture, $":{FormatString}");
                _ = stringBuilder.Append('}');
                return string.Format(provider, stringBuilder.ToString(), arg0);
            }
        }
        /// <summary>
        /// Defines segment naming convention.
        /// </summary>
        [Flags]
        private enum SegmentNamingConvention
        {
            /// <summary>
            /// Unknown naming convention.
            /// </summary>
            None = 0,
            /// <summary>
            /// Using only `ASCII digits` in the naming.
            /// </summary>
            AsciiDigit = 1,
            /// <summary>
            /// Using only named format in naming.
            /// </summary>
            Named = 2,
            /// <summary>
            /// Mixed naming convention.
            /// </summary>
            Mixed = AsciiDigit | Named
        }
    }
}