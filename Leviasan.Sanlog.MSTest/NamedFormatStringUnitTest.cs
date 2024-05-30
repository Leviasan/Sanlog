using System.Globalization;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class NamedFormatStringUnitTest
    {
        private static readonly DateTime DateTimeValue = new(2024, 5, 22, 23, 56, 18);
        private static readonly StringComparison StringComparisonValue = StringComparison.Ordinal;

        [TestMethod]
        public void ParseOneItemZeroAlignmentNullFormat()
        {
            var namedFormatString = NamedFormatString.Parse("DateTime: {DateTime}");

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
            var namedFormatString = NamedFormatString.Parse("DateTime: {DateTime:Y}");

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
            var namedFormatString = NamedFormatString.Parse("DateTime: {DateTime,-22:Y}");

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
            var namedFormatString = NamedFormatString.Parse("Year month: {DateTime:Y}. StringComparison: {StringComparison:D}. Sortable date/time: {DateTime:s}.");

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
    }
}