using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sanlog.MSTest
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "Unit-test class must be public")]
    public sealed class FormattedLogValuesFormatterUnitTest
    {
        [TestMethod]
        public void ConstructorDictionary()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "Login", "some_username" },
                { "Password", "some_password" },
                { FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}." }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary);
            Assert.IsTrue(formatter.IndexOf(FormattedLogValuesFormatter.OriginalFormat) != 1);
            Assert.IsNull(formatter.CultureInfo);
            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.SensitiveConfiguration.AddSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.IsTrue(formatter.SensitiveConfiguration.IsSensitive(SensitiveKeyType.SegmentName, "Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());

            Assert.AreEqual("some_password", formatter.GetObjectAsString(1, false).Value);
            Assert.AreEqual("[Redacted]", formatter.GetObjectAsString(1, true).Value);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter.GetObjectAsString(3, true));
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter.GetObjectAsString(null!, true));
            _ = Assert.ThrowsException<InvalidOperationException>(() => formatter.GetObjectAsString("InvalidKey", true));
        }
        [TestMethod]
        public void ConstructorSingleFormat()
        {
            var datetime = new DateTime(2024, 05, 22, 23, 56, 18);
            var formatter = new FormattedLogValuesFormatter("Timestamp: {Timestamp:Y}", datetime)
            {
                CultureInfo = CultureInfo.InvariantCulture
            };
            Assert.AreEqual("Timestamp: 2024 May", formatter.ToString());
            Assert.AreEqual(datetime.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString(0, false).Value);
            Assert.AreEqual(datetime.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString(0, true).Value);
        }
        [TestMethod]
        public void ConstructorMultiFormat()
        {
            var datetime = new DateTime(2024, 05, 22, 23, 56, 18);
            var formatter = new FormattedLogValuesFormatter("Year month: {Timestamp,9:Y}. StringComparison: {StringComparison:G}. Sortable date/time: {Timestamp:s}.", datetime, StringComparison.Ordinal, "extended_params")
            {
                CultureInfo = CultureInfo.InvariantCulture
            };
            Assert.AreEqual("Year month:  2024 May. StringComparison: Ordinal. Sortable date/time: 2024-05-22T23:56:18.", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorFormatException()
        {
            _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter("Login: {{Login}. Password: {Password}.", "some_username", "some_password"));
            _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter("Login: {2147483648}. Password: {0}.", "some_username", "some_password"));
        }
        [TestMethod]
        [DataRow("", MathF.PI, "3.14159274")]
        [DataRow("uk-ua", MathF.PI, "3,14159274")]
        [DataRow("", Math.PI, "3.1415926535897931")]
        [DataRow("uk-ua", Math.PI, "3,1415926535897931")]
        public void FormatFloatingPointNumber(string culture, object value, string expected)
        {
            var cultureInfo = CultureInfo.GetCultureInfo(culture);
            var formatter = new FormattedLogValuesFormatter("FormatFloatingPointNumber: {FormatFloatingPointNumber}.", value)
            {
                CultureInfo = cultureInfo
            };
            Assert.AreEqual(cultureInfo, formatter.CultureInfo);
            Assert.IsTrue(formatter.IndexOf(FormattedLogValuesFormatter.OriginalFormat) != -1);
            Assert.AreEqual(expected, formatter.GetObjectAsString("FormatFloatingPointNumber", true).Value);
        }
        [TestMethod]
        public void FormatOverrideType()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "Int32Value", 1 },
                { "NullValue", null },
                { "ShortArray", new short[10] },
                { "EnumValue", StringComparison.Ordinal },
                { "DateTimeValue", new DateTime(2024, 12, 3, 18, 42, 32, DateTimeKind.Utc) },
                { "DateTimeOffsetValue", new DateTimeOffset(2024, 12, 3, 18, 42, 32, TimeSpan.Zero) },
                { "DictionaryValue", new Dictionary<string, string> { { "Password", "some_password" } } }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary);
            Assert.IsTrue(formatter.IndexOf(FormattedLogValuesFormatter.OriginalFormat) == -1);
            Assert.IsTrue(formatter.SensitiveConfiguration.AddSensitive(SensitiveKeyType.CollapsePrimitive, "ShortArray"));
            Assert.IsTrue(formatter.SensitiveConfiguration.AddSensitive(SensitiveKeyType.DictionaryEntry, "Password"));
            // Non-redacted
            Assert.AreEqual("1", formatter.GetObjectAsString("Int32Value", false).Value);
            Assert.AreEqual("(null)", formatter.GetObjectAsString("NullValue", false).Value);
            Assert.AreEqual("[0, 0, 0, 0, 0, 0, 0, 0, 0, 0]", formatter.GetObjectAsString("ShortArray", false).Value);
            Assert.AreEqual("4", formatter.GetObjectAsString("EnumValue", false).Value);
            Assert.AreEqual("2024-12-03T18:42:32.0000000Z", formatter.GetObjectAsString("DateTimeValue", false).Value);
            Assert.AreEqual("2024-12-03T18:42:32.0000000+00:00", formatter.GetObjectAsString("DateTimeOffsetValue", false).Value);
            Assert.AreEqual("[[Password, some_password]]", formatter.GetObjectAsString("DictionaryValue", false).Value);
            // Redacted
            Assert.AreEqual("[*10 Int16*]", formatter.GetObjectAsString("ShortArray", true).Value);
            Assert.AreEqual("[[Password, [Redacted]]]", formatter.GetObjectAsString("DictionaryValue", true).Value);
        }
    }
}