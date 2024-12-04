using System.Collections;
using System.Globalization;

namespace Sanlog.MSTest
{
    [TestClass]
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
            Assert.IsTrue(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.IsNull(formatter.CultureInfo);
            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.SensitiveConfiguration.Add(typeof(object), "Password"));
            Assert.IsTrue(formatter.SensitiveConfiguration.Contains(typeof(object), "Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());

            Assert.AreEqual("some_password", formatter.GetObjectAsString(1, false).Value);
            Assert.AreEqual("[Redacted]", formatter.GetObjectAsString(1, true).Value);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter.GetObjectAsString(3, true));
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter.GetObjectAsString(null!, true));
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter.GetObjectAsString("InvalidKey", true));
        }
        [TestMethod]
        public void ConstructorSingleFormat()
        {
            var datetime = new DateTime(2024, 05, 22, 23, 56, 18);
            var formatter = new FormattedLogValuesFormatter("DateTime: {DateTime:Y}", datetime)
            {
                CultureInfo = CultureInfo.InvariantCulture
            };
            Assert.AreEqual("DateTime: 2024 May", formatter.ToString());
            Assert.AreEqual(datetime.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString(0, false).Value);
            Assert.AreEqual(datetime.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString(0, true).Value);
        }
        [TestMethod]
        public void ConstructorMultiFormat()
        {
            var datetime = new DateTime(2024, 05, 22, 23, 56, 18);
            var formatter = new FormattedLogValuesFormatter("Year month: {DateTime,9:Y}. StringComparison: {StringComparison:G}. Sortable date/time: {DateTime:s}.", datetime, StringComparison.Ordinal, "extended_params")
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
            Assert.IsTrue(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
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
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual("1", formatter.GetObjectAsString("Int32Value", false).Value);
            Assert.AreEqual("(null)", formatter.GetObjectAsString("NullValue", false).Value);
            Assert.AreEqual("[0, 0, 0, 0, 0, 0, 0, 0, 0, 0]", formatter.GetObjectAsString("ShortArray", false).Value);
            Assert.AreEqual("4", formatter.GetObjectAsString("EnumValue", false).Value);
            Assert.AreEqual("2024-12-03T18:42:32.0000000Z", formatter.GetObjectAsString("DateTimeValue", false).Value);
            Assert.AreEqual("2024-12-03T18:42:32.0000000+00:00", formatter.GetObjectAsString("DateTimeOffsetValue", false).Value);
            Assert.AreEqual("[[Password, some_password]]", formatter.GetObjectAsString("DictionaryValue", false).Value);
            formatter.FormatPrimitiveArray = true;
            Assert.AreEqual("[*10 Int16*]", formatter.GetObjectAsString("ShortArray", false).Value);
            Assert.IsTrue(formatter.SensitiveConfiguration.Add(typeof(DictionaryEntry), "Password"));
            Assert.AreEqual("[[Password, [Redacted]]]", formatter.GetObjectAsString("DictionaryValue", true).Value);
        }
        [TestMethod]
        public void ExtendedArgs()
        {
            var formatter = new FormattedLogValuesFormatter("Usual string", 1, 2, 3);
            Assert.AreEqual(1, formatter.GetObject(0, false).Value);
            Assert.AreEqual("args[0]", formatter.GetObject(0, false).Key);
            Assert.AreEqual(2, formatter.GetObject(1, false).Value);
            Assert.AreEqual("args[1]", formatter.GetObject(1, false).Key);
            Assert.AreEqual(3, formatter.GetObject(2, false).Value);
            Assert.AreEqual("args[2]", formatter.GetObject(2, false).Key);
            Assert.AreEqual("Usual string", formatter.GetObject(3, false).Value);
            Assert.AreEqual("Usual string", formatter.GetObjectAsString(FormattedLogValuesFormatter.OriginalFormat, false).Value);
        }
    }
}