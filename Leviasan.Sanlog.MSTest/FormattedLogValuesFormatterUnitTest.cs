using System.Globalization;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class FormattedLogValuesFormatterUnitTest
    {
        private static readonly DateTime DateTimeValue = new(2024, 5, 22, 23, 56, 18);

        [TestMethod]
        public void ConstructorPropertyListFormattedLogValues()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>("Login", "some_username") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
                { KeyValuePair.Create<string, object?>("DateTime", DateTimeValue) },
                { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "DateTime: {DateTime}. Login: {Login}. Password: {Password}.") },
            };
            var formatter = new FormattedLogValuesFormatter(list, null);
            Assert.IsTrue(formatter.HasOriginalFormat);
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", null)}. Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", null)}. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorPropertyListFormattedLogValuesNotOrdered()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "DateTime: {DateTime}. Login: {Login}. Password: {Password}.") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
                { KeyValuePair.Create<string, object?>("DateTime", DateTimeValue) },
                { KeyValuePair.Create<string, object?>("Login", "some_username") }
            };
            var formatter = new FormattedLogValuesFormatter(list, null);
            Assert.IsTrue(formatter.HasOriginalFormat);
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", null)}. Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", null)}. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorPropertyListWithoutOriginalFormat()
        {
            var formatter = new FormattedLogValuesFormatter(new List<KeyValuePair<string, object?>>(), null);
            Assert.IsFalse(formatter.HasOriginalFormat);
            Assert.AreEqual(FormattedLogValuesFormatter.NullFormat, formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringCurrentCulture()
        {
            var value = 123.4D;
            var formatter = new FormattedLogValuesFormatter(null, "DateTime: {DateTime}. Login: {Login}. Password: {Password}. Double: {Double}.", DateTimeValue, "some_username", "some_password", value);
            Assert.IsTrue(formatter.HasOriginalFormat);
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", null)}. Login: some_username. Password: some_password. Double: {value.ToString(null, null)}.", formatter.ToString());
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", null)}. Login: some_username. Password: [Redacted]. Double: {value.ToString(null, null)}.", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringInvariantCulture()
        {
            var formatter = new FormattedLogValuesFormatter(CultureInfo.InvariantCulture, "DateTime: {DateTime}. Login: {Login}. Password: {Password}. Double: {Double}.", DateTimeValue, "some_username", "some_password", 123.4D);
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", CultureInfo.InvariantCulture)}. Login: some_username. Password: [Redacted]. Double: 123.4.", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringSpecifiedCulture()
        {
            var formatProvider = CultureInfo.GetCultureInfo("uk-ua");
            var formatter = new FormattedLogValuesFormatter(formatProvider, "DateTime: {DateTime}. Login: {Login}. Password: {Password}. Double: {Double}.", DateTimeValue, "some_username", "some_password", 123.4D);
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {DateTimeValue.ToString("O", formatProvider)}. Login: some_username. Password: [Redacted]. Double: 123,4.", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringFormatException() => _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter(null, "Login: {{Login}. Password: {Password}.", "some_username", "some_password"));
        [TestMethod]
        public void IndexersGetValue()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));

            Assert.AreEqual("some_username", formatter[0].Value);
            Assert.AreEqual("[Redacted]", formatter[1].Value);
            Assert.AreEqual("some_username", formatter["Login"].Value);
            Assert.AreEqual("[Redacted]", formatter["Password"].Value);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter[3]);
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter[null!]);
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter["InvalidKey"]);
        }
        [TestMethod]
        public void GetObjectAsString()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));

            Assert.AreEqual("[Redacted]", formatter.GetObjectAsString(1, true).Value);
            Assert.AreEqual("some_password", formatter.GetObjectAsString(1, false).Value);
            Assert.AreEqual("[Redacted]", formatter.GetObjectAsString("Password", true).Value);
            Assert.AreEqual("some_password", formatter.GetObjectAsString("Password", false).Value);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter.GetObjectAsString(3, true));
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter.GetObjectAsString(null!, true));
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter.GetObjectAsString("InvalidKey", true));
        }
        [TestMethod]
        public void RegisterSensitiveDataIgnoreOriginalFormat()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            _ = formatter.RegisterSensitiveData([FormattedLogValuesFormatter.OriginalFormat, "Password"]);
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
            var dateTime = DateTime.UtcNow;
            var formatter = new FormattedLogValuesFormatter(null, "Enumerable: {Enumerable}. Login: {Login}. Password: {Password}.", new object?[3] { dateTime, null, 2 }, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"Enumerable: {dateTime.ToString("O", CultureInfo.InvariantCulture)}, (null), 2. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void NullParameterDictionaryEntryValue()
        {
            var dateTime = DateTime.UtcNow;
            var formatter = new FormattedLogValuesFormatter(null, "Dictionary: {Enumerable}. Login: {Login}. Password: {Password}.", new Dictionary<string, object?> { { "NotNull", dateTime }, { "Nullable", null } }, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"Dictionary: [NotNull, {dateTime.ToString("O", CultureInfo.InvariantCulture)}], [Nullable, (null)]. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
    }
}