using System;
using System.Globalization;
using System.Text;

namespace Sanlog.MSTest
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Unit-test class must be public")]
    public sealed class MessageTemplateUnitTest
    {
        private static readonly DateTime DateTimeValue = new(2024, 5, 22, 23, 56, 18);
        private const StringComparison StringComparisonValue = StringComparison.Ordinal;

        [TestMethod]
        public void FormatEmpty()
        {
            var messageTemplate = new MessageTemplate("TimeStamp: {TimeStamp}");
            Assert.AreEqual("TimeStamp", messageTemplate[0]);
            Assert.AreEqual("TimeStamp: {0}", messageTemplate.CompositeFormat.Format);
            Assert.AreEqual("TimeStamp: 05/22/2024 23:56:18", messageTemplate.Format(CultureInfo.InvariantCulture, DateTimeValue));
        }
        [TestMethod]
        public void FormatFormatString()
        {
            var messageTemplate = new MessageTemplate("TimeStamp: {TimeStamp:Y}");
            Assert.AreEqual("TimeStamp", messageTemplate[0]);
            Assert.AreEqual("TimeStamp: {0:Y}", messageTemplate.CompositeFormat.Format);
            Assert.AreEqual("TimeStamp: 2024 May", messageTemplate.Format(CultureInfo.InvariantCulture, DateTimeValue));
        }
        [TestMethod]
        public void FormatAlignmentAndFormatString()
        {
            var messageTemplate = new MessageTemplate("TimeStamp: {TimeStamp,-22:Y}");
            Assert.AreEqual("TimeStamp", messageTemplate[0]);
            Assert.AreEqual("TimeStamp: {0,-22:Y}", messageTemplate.CompositeFormat.Format);
            Assert.AreEqual("TimeStamp: 2024 May              ", messageTemplate.Format(CultureInfo.InvariantCulture, DateTimeValue));
        }
        [TestMethod]
        public void FormatThreeItemsWithTwoEqualsAndDifferentFormats()
        {
            var messageTemplate = new MessageTemplate("Year month: {TimeStamp:Y}. StringComparison: {StringComparison:D}. Sortable date/time: {TimeStamp:s}.");
            Assert.AreEqual("TimeStamp", messageTemplate[0]);
            Assert.AreEqual("StringComparison", messageTemplate[1]);
            Assert.AreEqual("TimeStamp", messageTemplate[2]);
            Assert.AreEqual("Year month: {0:Y}. StringComparison: {1:D}. Sortable date/time: {0:s}.", messageTemplate.CompositeFormat.Format);
            Assert.AreEqual("Year month: 2024 May. StringComparison: 4. Sortable date/time: 2024-05-22T23:56:18.", messageTemplate.Format(CultureInfo.InvariantCulture, DateTimeValue, StringComparisonValue));
        }
        [TestMethod]
        public void IndexComponentSingle()
        {
            var messageTemplate = new MessageTemplate("0x{0:X} {0:E} {0:N}");
            Assert.AreEqual("0x7FFFFFFFFFFFFFFF 9.223372E+018 9,223,372,036,854,775,807.00", messageTemplate.Format(CultureInfo.InvariantCulture, long.MaxValue));
            messageTemplate = new MessageTemplate("{TimeStamp:dddd MMMM}");
            Assert.AreEqual(DateTime.Now.ToString("dddd MMMM", CultureInfo.InvariantCulture), messageTemplate.Format(CultureInfo.InvariantCulture, DateTime.Now));
        }
        [TestMethod]
        public void IndexComponentNotOrdered()
        {
            var format = "{1} {0} {2}";
            var messageTemplate = new MessageTemplate(format);
            Assert.AreEqual("1", messageTemplate[0]);
            Assert.AreEqual("0", messageTemplate[1]);
            Assert.AreEqual("2", messageTemplate[2]);
            Assert.AreEqual(string.Format(CultureInfo.InvariantCulture, CompositeFormat.Parse(format), 0, 1, 2), messageTemplate.Format(CultureInfo.InvariantCulture, 0, 1, 2));
        }
        [TestMethod]
        public void InvalidInputStringFormatException()
        {
            var formatException = Assert.ThrowsException<FormatException>(() => new MessageTemplate("{1} {InvalidName} {2}"));
            Assert.AreEqual("The input string was not in the correct format. Fail to parse near offset 5. The mixed argument names are not supported.", formatException.Message);
            formatException = Assert.ThrowsException<FormatException>(() => new MessageTemplate("{InvalidName} {0} {2}"));
            Assert.AreEqual("The input string was not in the correct format. Fail to parse near offset 15. The mixed argument names are not supported.", formatException.Message);
            formatException = Assert.ThrowsException<FormatException>(() => new MessageTemplate("{1} {0} {InvalidName}"));
            Assert.AreEqual("The input string was not in the correct format. Fail to parse near offset 9. The mixed argument names are not supported.", formatException.Message);
            formatException = Assert.ThrowsException<FormatException>(() => new MessageTemplate("{1} {0} {2S}"));
            Assert.AreEqual("The input string was not in the correct format. Fail to parse near offset 9. The mixed argument names are not supported.", formatException.Message);
        }
        [TestMethod]
        public void AlignmentComponent()
        {
            var messageTemplate = new MessageTemplate("{0,-20} {1,5:N1}");
            Assert.AreEqual("Adam                  40.0", messageTemplate.Format(CultureInfo.InvariantCulture, "Adam", 40));
        }
        [TestMethod]
        public void EscapingBraces()
        {
            var messageTemplate = new MessageTemplate("{{{0:D}}}");
            Assert.AreEqual("{6324}", messageTemplate.Format(CultureInfo.InvariantCulture, 6324));
        }
        [TestMethod]
        public void MixedNamingConvention()
        {
            var messageTemplate = new MessageTemplate("{A1} {0A} {CC} {A1,5:X2}");
            Assert.AreEqual("15 1 2    0F", messageTemplate.Format(CultureInfo.InvariantCulture, 15, 1, 2));
        }
    }
}