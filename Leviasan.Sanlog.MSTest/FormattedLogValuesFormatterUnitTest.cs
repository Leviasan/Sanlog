using System.Collections;
using System.Globalization;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class FormattedLogValuesFormatterUnitTest
    {
        [TestMethod]
        public void Constructor()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "Login", "some_username" },
                { "Password", "some_password" },
                { FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}." }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsTrue(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.IsNull(formatter.CultureInfo);
            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.BuildString());
            Assert.IsTrue(formatter.Sensitive.Add(typeof(object), "Password"));
            Assert.IsTrue(formatter.Sensitive.Contains(typeof(object), "Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.BuildString());

            Assert.AreEqual("some_password", formatter.GetObjectAsString(1, false).Value);
            Assert.AreEqual("[Redacted]", formatter.GetObjectAsString(1, true).Value);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter.GetObjectAsString(3, true));
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter.GetObjectAsString(null!, true));
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter.GetObjectAsString("InvalidKey", true));
        }
        [TestMethod]
        public void ConstructorFormatException()
        {
            _ = Assert.ThrowsException<FormatException>(() => FormattedLogValuesFormatter.Create("Login: {{Login}. Password: {Password}.", "some_username", "some_password"));
            _ = Assert.ThrowsException<FormatException>(() => FormattedLogValuesFormatter.Create("Login: {2147483648}. Password: {0}.", "some_username", "some_password"));
        }
        [TestMethod]
        [DataRow("", MathF.PI, "3.14159274")]
        [DataRow("uk-ua", MathF.PI, "3,14159274")]
        [DataRow("", Math.PI, "3.1415926535897931")]
        [DataRow("uk-ua", Math.PI, "3,1415926535897931")]
        public void FormatValue(string culture, object value, string expected)
        {
            CultureInfo? cultureInfo = CultureInfo.GetCultureInfo(culture);
            var formatter = FormattedLogValuesFormatter.Create(cultureInfo, null, "BuildString: {FormatValue}.", value);
            Assert.IsTrue(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual(cultureInfo, formatter.CultureInfo);
            Assert.AreEqual(expected, formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatEnum()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "FormatValue", StringComparison.Ordinal }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual(StringComparison.Ordinal.ToString("D"), formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatNullValue()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "FormatValue", null }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual(FormattedLogValuesFormatter.NullValue, formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatDateTime()
        {
            var datetime = DateTime.Now;
            var dictionary = new Dictionary<string, object?>
            {
                { "FormatValue", datetime }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual(datetime.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatDateTimeOffset()
        {
            var offset = DateTimeOffset.Now;
            var dictionary = new Dictionary<string, object?>
            {
                { "FormatValue", offset }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual(offset.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatInt64Array()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "FormatValue", new long[10] }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual("[0, 0, 0, 0, 0, 0, 0, 0, 0, 0]", formatter.GetObjectAsString("FormatValue", true).Value);
            formatter.PrimitiveTypeArray = true;
            Assert.AreEqual("[*10 Int64*]", formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatListByte()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "FormatValue", new List<byte>(new byte[10]) }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual("[0, 0, 0, 0, 0, 0, 0, 0, 0, 0]", formatter.GetObjectAsString("FormatValue", true).Value);
            formatter.PrimitiveTypeArray = true;
            Assert.AreEqual("[*10 Byte*]", formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatDictionary()
        {
            var dictionary = new Dictionary<string, object?>
            {
                {
                    "FormatValue",
                    new Dictionary<string, object?>
                    {
                        { "NotNull", 1 },
                        { "NullValue", null },
                        { "ShortArray", new short[10] }
                    }
                }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual("[[NotNull, 1], [NullValue, (null)], [ShortArray, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]]]", formatter.GetObjectAsString("FormatValue", true).Value);
            formatter.PrimitiveTypeArray = true;
            Assert.AreEqual("[[NotNull, 1], [NullValue, (null)], [ShortArray, [*10 Int16*]]]", formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatEnumerable()
        {
            var dictionary = new Dictionary<string, object?>
            {
                {
                    "FormatValue",
                    new object?[4]
                    {
                        1,
                        null,
                        new int[10],
                        new Dictionary<string, string>
                        {
                            { "Password", "some_password" }
                        }
                    }
                }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.AreEqual("[1, (null), [0, 0, 0, 0, 0, 0, 0, 0, 0, 0], [[Password, some_password]]]", formatter.GetObjectAsString("FormatValue", true).Value);
            Assert.IsTrue(formatter.Sensitive.Add(typeof(DictionaryEntry), "Password"));
            formatter.PrimitiveTypeArray = true;
            Assert.AreEqual("[1, (null), [*10 Int32*], [[Password, [Redacted]]]]", formatter.GetObjectAsString("FormatValue", true).Value);
        }
        [TestMethod]
        public void FormatWithFormatString()
        {
            var datetime = new DateTime(2024, 05, 22, 23, 56, 18);
            var formatter = FormattedLogValuesFormatter.Create(CultureInfo.InvariantCulture, null, "DateTime: {DateTime:Y}", datetime);
            Assert.AreEqual("DateTime: 2024 May", formatter.BuildString());
            Assert.AreEqual(datetime.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString(0, false).Value);
            Assert.AreEqual(datetime.ToString("O", CultureInfo.InvariantCulture), formatter.GetObjectAsString(0, true).Value);
        }
        [TestMethod]
        public void FormatWithDifferentFormatString()
        {
            var datetime = new DateTime(2024, 05, 22, 23, 56, 18);
            var formatter = FormattedLogValuesFormatter.Create(CultureInfo.InvariantCulture, null, "Year month: {DateTime,9:Y}. StringComparison: {StringComparison:G}. Sortable date/time: {DateTime:s}.", datetime, StringComparison.Ordinal);
            Assert.AreEqual("Year month:  2024 May. StringComparison: Ordinal. Sortable date/time: 2024-05-22T23:56:18.", formatter.BuildString());
        }
    }
}