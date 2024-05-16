using System.Globalization;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class FormattedLogValuesFormatterUnitTest
    {
        [TestMethod]
        public void ConstructorPropertyListFormattedLogValues()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>("Login", "some_username") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
                { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}.") },
            };
            var formatter = new FormattedLogValuesFormatter(null, list, null);
            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorPropertyListFormattedLogValuesNotOrdered()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}.") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
                { KeyValuePair.Create<string, object?>("Login", "some_username") }
            };
            var formatter = new FormattedLogValuesFormatter(null, list, null);
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorPropertyListWithoutOriginalFormat()
        {
            var formatter = new FormattedLogValuesFormatter(null, new List<KeyValuePair<string, object?>>(), () => "Invoked default formatter");
            Assert.AreEqual("Invoked default formatter", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorPropertyListEmptyWithoutFormatter()
        {
            var formatter = new FormattedLogValuesFormatter(null, new List<KeyValuePair<string, object?>>(), null);
            Assert.AreEqual("[null]", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringCurrentCulture()
        {
            var value = 123.4D;
            var dateTime = DateTime.UtcNow;
            var formatter = new FormattedLogValuesFormatter(null, "DateTime: {DateTime}. Login: {Login}. Password: {Password}. Double: {Double}.", dateTime, "some_username", "some_password", value);
            Assert.AreEqual($"DateTime: {dateTime.ToString("s", null)}. Login: some_username. Password: some_password. Double: {value.ToString(null, null)}.", formatter.ToString());
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {dateTime.ToString("s", null)}. Login: some_username. Password: [Redacted]. Double: {value.ToString(null, null)}.", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringInvariantCulture()
        {
            var dateTime = DateTime.UtcNow;
            var formatter = new FormattedLogValuesFormatter(CultureInfo.InvariantCulture, "DateTime: {DateTime}. Login: {Login}. Password: {Password}. Double: {Double}.", dateTime, "some_username", "some_password", 123.4D);
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {dateTime.ToString("s", CultureInfo.InvariantCulture)}. Login: some_username. Password: [Redacted]. Double: 123.4.", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringSpecifiedCulture()
        {
            var dateTime = DateTime.UtcNow;
            var formatProvider = CultureInfo.GetCultureInfo("uk-ua");
            var formatter = new FormattedLogValuesFormatter(formatProvider, "DateTime: {DateTime}. Login: {Login}. Password: {Password}. Double: {Double}.", dateTime, "some_username", "some_password", 123.4D);
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {dateTime.ToString("s", formatProvider)}. Login: some_username. Password: [Redacted]. Double: 123,4.", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringFormatException()
        {
            _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter(null, "Login: {{Login}. Password: {Password}.", "some_username", "some_password"));
        }
        [TestMethod]
        public void IndexersGetValue()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));

            Assert.AreEqual("some_username", formatter[0].Value);
            Assert.AreEqual("[Redacted]", formatter[1].Value);
            Assert.AreEqual("some_username", formatter["Login"]);
            Assert.AreEqual("[Redacted]", formatter["Password"]);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter[3]);
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter[null!]);
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter["InvalidKey"]);
        }
        [TestMethod]
        public void GetData()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));

            Assert.AreEqual("[Redacted]", formatter.GetData(1, true).Value);
            Assert.AreEqual("some_password", formatter.GetData(1, false).Value);
            Assert.AreEqual("[Redacted]", formatter.GetData("Password", true).Value);
            Assert.AreEqual("some_password", formatter.GetData("Password", false).Value);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter.GetData(3, true));
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter.GetData(null!, true));
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter.GetData("InvalidKey", true));
        }
        [TestMethod]
        public void RegisterSensitiveDataIgnoreOriginalFormat()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            formatter.RegisterSensitiveData([FormattedLogValuesFormatter.OriginalFormat, "Password"]);
            Assert.IsFalse(formatter.IsSensitiveData(FormattedLogValuesFormatter.OriginalFormat));
            Assert.IsTrue(formatter.IsSensitiveData("Password"));
        }
        [TestMethod]
        public void NullParameterValue()
        {
            var formatter = new FormattedLogValuesFormatter(null, "DateTime: {DateTime}. Login: {Login}. Password: {Password}.", null, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: (null). Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void NullParameterEnumerableElement()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Enumerable: {Enumerable}. Login: {Login}. Password: {Password}.", new object?[3] { 1, null, 2 }, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"Enumerable: 1, (null), 2. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void NullParameterDictionaryEntryValue()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Dictionary: {Enumerable}. Login: {Login}. Password: {Password}.", new Dictionary<string, object?> { { "NotNull", 1 }, { "Nullable", null } }, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"Dictionary: [NotNull, 1], [Nullable, (null)]. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
    }
}