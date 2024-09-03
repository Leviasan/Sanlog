using System.Collections;
using System.Globalization;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class FormattedLogValuesFormatterUnitTest
    {
        private static readonly DateTime DateTimeValue = new(2024, 5, 22, 23, 56, 18);
        private static readonly DateTimeOffset DateTimeOffsetValue = new(DateTimeValue);
        private static readonly StringComparison StringComparisonValue = StringComparison.Ordinal;

        [TestMethod]
        public void CtorListOrdered()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { "Login", "some_username" },
                { "Password", "some_password" },
                { FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}." }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsTrue(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));

            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.SensitiveConfiguration.Add(typeof(object), "Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void CtorListOrderedNo()
        {
            var dictionary = new Dictionary<string, object?>
            {
                { FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}." },
                { "Password", "some_password" },
                { "Login", "some_username" }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsTrue(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));

            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.AreEqual(2, formatter.SensitiveConfiguration.Add(typeof(object), [FormattedLogValuesFormatter.OriginalFormat, "Password"]));
            Assert.IsTrue(formatter.SensitiveConfiguration.Contains(typeof(object), FormattedLogValuesFormatter.OriginalFormat));
            Assert.IsTrue(formatter.SensitiveConfiguration.Contains(typeof(object), "Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        [DataRow("")]
        [DataRow("uk-ua")]
        public void CtorListProviderDependence(string culture)
        {
            CultureInfo? cultureInfo = CultureInfo.GetCultureInfo(culture);
            var dictionary = new Dictionary<string, object?>
            {
                { "Single", MathF.PI },
                { "Double", Math.PI },
                { "Enum", StringComparisonValue },
                { "DateTime", DateTimeValue },
                { "DateTimeOffset", DateTimeOffsetValue },
                { "NullValue", null },
                { "Enumerable", new object?[4] { 1, null, 2, new Dictionary<string, object?> { { "Password", "some_password" } } } },
                { "Dictionary", new Dictionary<string, object?> { { "NotNull", 1 }, { "NullValue", null } } },
                { "ByteList", new List<byte>(new byte[125]) },
                { "ByteArray", new byte[125] }
            };
            var formatter = new FormattedLogValuesFormatter(dictionary, null);
            Assert.IsFalse(formatter.ContainsKey(FormattedLogValuesFormatter.OriginalFormat));
            Assert.IsTrue(formatter.SensitiveConfiguration.Add(typeof(DictionaryEntry), "Password"));
            formatter.CultureInfo = cultureInfo;

            Assert.AreEqual(MathF.PI.ToString("G9", cultureInfo), formatter.GetObjectAsString("Single", true).Value);
            Assert.AreEqual(Math.PI.ToString("G17", cultureInfo), formatter.GetObjectAsString("Double", true).Value);
            Assert.AreEqual(StringComparisonValue.ToString("D"), formatter.GetObjectAsString("Enum", true).Value);
            Assert.AreEqual(DateTimeValue.ToString("O", cultureInfo), formatter.GetObjectAsString("DateTime", true).Value);
            Assert.AreEqual(DateTimeOffsetValue.ToString("O", cultureInfo), formatter.GetObjectAsString("DateTimeOffset", true).Value);
            Assert.AreEqual(FormattedLogValuesFormatter.NullValue, formatter.GetObjectAsString("NullValue", true).Value);
            Assert.AreEqual("[1, (null), 2, [[Password, [Redacted]]]]", formatter.GetObjectAsString("Enumerable", true).Value);
            Assert.AreEqual("[[NotNull, 1], [NullValue, (null)]]", formatter.GetObjectAsString("Dictionary", true).Value);
            Assert.AreEqual("[*125 bytes*]", formatter.GetObjectAsString("ByteList", true).Value);
            Assert.AreEqual("[*125 bytes*]", formatter.GetObjectAsString("ByteArray", true).Value);
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