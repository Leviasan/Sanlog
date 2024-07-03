using System.Globalization;
using System.Text;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class LogMessageFormatterUnitTest
    {
        private static readonly DateTime DateTimeValue = new(2024, 5, 22, 23, 56, 18);
        private static readonly StringComparison StringComparisonValue = StringComparison.Ordinal;

        [TestMethod]
        public void ParseOneItemZeroAlignmentNullFormat()
        {
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse("DateTime: {DateTime}");

            Assert.AreEqual(0, namedFormatString.Segments[0].Index);
            Assert.AreEqual("DateTime", namedFormatString.Segments[0].Name);
            Assert.AreEqual(0, namedFormatString.Segments[0].Alignment);
            Assert.IsNull(namedFormatString.Segments[0].FormatString);

            Assert.AreEqual("DateTime: {0}", namedFormatString.CompositeFormat.Format);
            Assert.AreEqual("05/22/2024 23:56:18", namedFormatString.Segments[0].Format(CultureInfo.InvariantCulture, DateTimeValue));
            Assert.AreEqual("DateTime: 05/22/2024 23:56:18", namedFormatString.Format(CultureInfo.InvariantCulture, DateTimeValue));
        }
        [TestMethod]
        public void ParseOneItemZeroAlignmentSpecifiedFormat()
        {
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse("DateTime: {DateTime:Y}");

            Assert.AreEqual(0, namedFormatString.Segments[0].Index);
            Assert.AreEqual("DateTime", namedFormatString.Segments[0].Name);
            Assert.AreEqual(0, namedFormatString.Segments[0].Alignment);
            Assert.AreEqual("Y", namedFormatString.Segments[0].FormatString);

            Assert.AreEqual("DateTime: {0:Y}", namedFormatString.CompositeFormat.Format);
            Assert.AreEqual("2024 May", namedFormatString.Segments[0].Format(CultureInfo.InvariantCulture, DateTimeValue));
            Assert.AreEqual("DateTime: 2024 May", namedFormatString.Format(CultureInfo.InvariantCulture, DateTimeValue));
        }
        [TestMethod]
        public void ParseOneItemNegativeAlignmentSpecifiedFormat()
        {
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse("DateTime: {DateTime,-22:Y}");

            Assert.AreEqual(0, namedFormatString.Segments[0].Index);
            Assert.AreEqual("DateTime", namedFormatString.Segments[0].Name);
            Assert.AreEqual(-22, namedFormatString.Segments[0].Alignment);
            Assert.AreEqual("Y", namedFormatString.Segments[0].FormatString);

            Assert.AreEqual("DateTime: {0,-22:Y}", namedFormatString.CompositeFormat.Format);
            Assert.AreEqual("2024 May              ", namedFormatString.Segments[0].Format(CultureInfo.InvariantCulture, DateTimeValue));
            Assert.AreEqual("DateTime: 2024 May              ", namedFormatString.Format(CultureInfo.InvariantCulture, DateTimeValue));
        }
        [TestMethod]
        public void ParseThreeItemsTwoEqualsDifferentFormats()
        {
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse("Year month: {DateTime:Y}. StringComparison: {StringComparison:D}. Sortable date/time: {DateTime:s}.");

            Assert.AreEqual(0, namedFormatString.Segments[0].Index);
            Assert.AreEqual("DateTime", namedFormatString.Segments[0].Name);
            Assert.AreEqual(0, namedFormatString.Segments[0].Alignment);
            Assert.AreEqual("Y", namedFormatString.Segments[0].FormatString);

            Assert.AreEqual(1, namedFormatString.Segments[1].Index);
            Assert.AreEqual("StringComparison", namedFormatString.Segments[1].Name);
            Assert.AreEqual(0, namedFormatString.Segments[1].Alignment);
            Assert.AreEqual("D", namedFormatString.Segments[1].FormatString);

            Assert.AreEqual(0, namedFormatString.Segments[2].Index);
            Assert.AreEqual("DateTime", namedFormatString.Segments[2].Name);
            Assert.AreEqual(0, namedFormatString.Segments[2].Alignment);
            Assert.AreEqual("s", namedFormatString.Segments[2].FormatString);

            Assert.AreEqual("Year month: {0:Y}. StringComparison: {1:D}. Sortable date/time: {0:s}.", namedFormatString.CompositeFormat.Format);
            Assert.AreEqual("2024 May", namedFormatString.Segments[0].Format(CultureInfo.InvariantCulture, DateTimeValue));
            Assert.AreEqual("4", namedFormatString.Segments[1].Format(CultureInfo.InvariantCulture, StringComparisonValue));
            Assert.AreEqual("2024-05-22T23:56:18", namedFormatString.Segments[2].Format(CultureInfo.InvariantCulture, DateTimeValue));
            Assert.AreEqual("Year month: 2024 May. StringComparison: 4. Sortable date/time: 2024-05-22T23:56:18.", namedFormatString.Format(CultureInfo.InvariantCulture, DateTimeValue, StringComparisonValue));
        }
        [TestMethod]
        public void IndexComponentSingle()
        {
            var format = FormattedLogValuesFormatter.LogMessageFormatter.Parse("0x{0:X} {0:E} {0:N}");
            Assert.AreEqual("0x7FFFFFFFFFFFFFFF 9.223372E+018 9,223,372,036,854,775,807.00", format.Format(CultureInfo.InvariantCulture, long.MaxValue));
            
            format = FormattedLogValuesFormatter.LogMessageFormatter.Parse("{DateTime:dddd MMMM}");
            Assert.AreEqual(DateTime.Now.ToString("dddd MMMM", CultureInfo.InvariantCulture), format.Format(CultureInfo.InvariantCulture, DateTime.Now));
        }
        [TestMethod]
        public void IndexComponentNotOrdered()
        {
            var format = "{1} {0} {2}";
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse(format);
            Assert.AreEqual("1", namedFormatString.Segments[0].Name);
            Assert.AreEqual("0", namedFormatString.Segments[1].Name);
            Assert.AreEqual("2", namedFormatString.Segments[2].Name);
            Assert.AreEqual(1, namedFormatString.Segments[0].Index);
            Assert.AreEqual(0, namedFormatString.Segments[1].Index);
            Assert.AreEqual(2, namedFormatString.Segments[2].Index);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(format), 0, 1, 2), namedFormatString.Format(CultureInfo.InvariantCulture, 0, 1, 2));
        }
        [TestMethod]
        public void InvalidInputStringFormatException()
        {
            var formatException = Assert.ThrowsException<FormatException>(() => FormattedLogValuesFormatter.LogMessageFormatter.Parse("{1} {InvalidName} {2}"));
            Assert.AreEqual("Input string was not in a correct format. Failure to parse near offset 5.", formatException.Message);
            formatException = Assert.ThrowsException<FormatException>(() => FormattedLogValuesFormatter.LogMessageFormatter.Parse("{InvalidName} {0} {2}"));
            Assert.AreEqual("Input string was not in a correct format. Failure to parse near offset 15.", formatException.Message);
            formatException = Assert.ThrowsException<FormatException>(() => FormattedLogValuesFormatter.LogMessageFormatter.Parse("{1} {0} {InvalidName}"));
            Assert.AreEqual("Input string was not in a correct format. Failure to parse near offset 9.", formatException.Message);
            formatException = Assert.ThrowsException<FormatException>(() => FormattedLogValuesFormatter.LogMessageFormatter.Parse("{1} {0} {2S}"));
            Assert.AreEqual("Input string was not in a correct format. Failure to parse near offset 9.", formatException.Message);
        }
        [TestMethod]
        public void AlignmentComponent()
        {
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse("{0,-20} {1,5:N1}");
            Assert.AreEqual("Adam                  40.0", namedFormatString.Format(CultureInfo.InvariantCulture, "Adam", 40));
        }
        [TestMethod]
        public void EscapingBraces()
        {
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse("{{{0:D}}}");
            Assert.AreEqual("{6324}", namedFormatString.Format(CultureInfo.InvariantCulture, 6324));
        }
        [TestMethod]
        public void MixedNamingConvention()
        {
            var namedFormatString = FormattedLogValuesFormatter.LogMessageFormatter.Parse("{A1} {0A} {CC} {A1,5:X2}");
            Assert.AreEqual("15 1 2    0F", namedFormatString.Format(CultureInfo.InvariantCulture, 15, 1, 2));
        }
    }
}