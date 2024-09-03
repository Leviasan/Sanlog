using System;
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
            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.SensitiveConfiguration.Add(typeof(object), "Password"));
            Assert.IsTrue(formatter.SensitiveConfiguration.Contains(typeof(object), "Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        [DataRow("", MathF.PI, "3.14159274")]
        [DataRow("uk-ua", MathF.PI, "3,14159274")]
        [DataRow("", Math.PI, "3.1415926535897931")]
        [DataRow("uk-ua", Math.PI, "3,1415926535897931")]
        public void FormatValue(string culture, object value, string expected)
        {
            CultureInfo? cultureInfo = CultureInfo.GetCultureInfo(culture);
            var formatter = FormattedLogValuesFormatter.Create(cultureInfo, null, "Format: {FormatValue}.", value);
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
            Assert.IsTrue(formatter.SensitiveConfiguration.Add(typeof(DictionaryEntry), "Password"));
            formatter.PrimitiveTypeArray = true;
            Assert.AreEqual("[1, (null), [*10 Int32*], [[Password, [Redacted]]]]", formatter.GetObjectAsString("FormatValue", true).Value);
        }
        /*
            [TestMethod]
            public void CtorNamedFormatUseInt32KeyName()
            {
                var formatter = new FormattedLogValuesFormatter(formatProvider: null, format: string.Empty, args: "some_username");
                Assert.IsFalse(formatter.HasOriginalFormat);

                Assert.AreEqual("0", formatter[0].Key);
                Assert.AreEqual("some_username", formatter[0].Value);
            }
            [TestMethod]
            public void CtorNamedInvalidFormat()
            {
                _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter(null, "Login: {{Login}. Password: {Password}.", "some_username", "some_password"));
            }
            [TestMethod]
            public void CtorNamedNegativeIndex()
            {
                _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter(null, "Login: {2147483648}. Password: {0}.", "some_username", "some_password"));
            }
            [TestMethod]
            public void GetObjectAsString()
            {
                var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
                Assert.IsTrue(formatter.HasOriginalFormat);

                Assert.IsTrue(formatter.RegisterSensitiveData(typeof(string), "Password"));
                Assert.AreEqual("[Redacted]", formatter.GetObjectAsString(1, true).Value);
                Assert.AreEqual("some_password", formatter.GetObjectAsString(1, false).Value);
                Assert.AreEqual("[Redacted]", formatter.GetObjectAsString("Password", true).Value);
                Assert.AreEqual("some_password", formatter.GetObjectAsString("Password", false).Value);

                _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter.GetObjectAsString(3, true));
                _ = Assert.ThrowsException<ArgumentNullException>(() => formatter.GetObjectAsString(null!, true));
                _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter.GetObjectAsString("InvalidKey", true));
            }
            [TestMethod]
            public void IDictionarySensitiveData()
            {
                var list = new List<KeyValuePair<string, object?>>
                {
                    { KeyValuePair.Create<string, object?>("Credential", new Dictionary<string, object?> { { "Username", "some_username" }, { "Password", "some_password" } }) },
                    { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "Credential: {Credential}.") }
                };
                var formatter = new FormattedLogValuesFormatter(list, null);

                Assert.AreEqual("Credential: [[Username, some_username], [Password, some_password]].", formatter.ToString());
                Assert.AreEqual("[[Username, some_username], [Password, some_password]]", formatter.GetObjectAsString("Credential", true).Value);
                Assert.AreEqual("[[Username, some_username], [Password, some_password]]", formatter.GetObjectAsString("Credential", false).Value);

                Assert.IsTrue(formatter.RegisterSensitiveData(typeof(DictionaryEntry), "Password"));
                Assert.AreEqual("Credential: [[Username, some_username], [Password, [Redacted]]].", formatter.ToString());
                Assert.AreEqual("[[Username, some_username], [Password, [Redacted]]]", formatter.GetObjectAsString("Credential", true).Value);
                Assert.AreEqual("[[Username, some_username], [Password, some_password]]", formatter.GetObjectAsString("Credential", false).Value);
            }
            [TestMethod]
            public void RegisterSensitiveDataTransaction()
            {
                var formatter = new FormattedLogValuesFormatter([], null);
                Assert.AreEqual(4, formatter.RegisterSensitiveData([KeyValuePair.Create<Type, HashSet<string>>(typeof(string), ["Username", "Password"]), KeyValuePair.Create<Type, HashSet<string>>(typeof(DictionaryEntry), ["Username", "Password"])]));

                formatter = new FormattedLogValuesFormatter([], null);
                Assert.ThrowsException<ArgumentNullException>(() => formatter.RegisterSensitiveData([KeyValuePair.Create<Type, HashSet<string>>(typeof(string), ["Username", "Password"]), KeyValuePair.Create<Type, HashSet<string>>(typeof(DictionaryEntry), [null, "Password"])]));
                Assert.AreEqual(false, formatter.IsSensitiveData(typeof(DictionaryEntry), "Username"));
            }
            [TestMethod]
            public void ParseOneItemZeroAlignmentSpecifiedFormat()
            {
                var formatter = new FormattedLogValuesFormatter(CultureInfo.InvariantCulture, "DateTime: {DateTime:Y}", DateTimeValue);
                Assert.AreEqual("DateTime: 2024 May", formatter.ToString());

                Assert.IsTrue(formatter.RegisterSensitiveData(typeof(string), "DateTime"));
                Assert.AreEqual("[Redacted]", formatter.GetObjectAsString(0, true).Value);
                Assert.AreEqual("2024-05-22T23:56:18.0000000", formatter.GetObjectAsString(0, false).Value);
                Assert.AreEqual("[Redacted]", formatter.GetObject(0, true).Value);
                Assert.AreEqual(DateTimeValue, formatter.GetObject(0, false).Value);
            }
            [TestMethod]
            public void ParseThreeItemsTwoEqualsDifferentFormats()
            {
                var formatter = new FormattedLogValuesFormatter(CultureInfo.InvariantCulture, "Year month: {DateTime:Y}. StringComparison: {StringComparison:D}. Sortable date/time: {DateTime:s}.", DateTimeValue, StringComparisonValue);
                Assert.AreEqual("Year month: 2024 May. StringComparison: 4. Sortable date/time: 2024-05-22T23:56:18.", formatter.ToString());
            }
        */
    }
}