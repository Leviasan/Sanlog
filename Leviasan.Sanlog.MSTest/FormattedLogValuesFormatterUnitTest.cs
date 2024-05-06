using System.Globalization;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class FormattedLogValuesFormatterUnitTest
    {
        [TestMethod]
        public void ConstructorOriginalPropertyList()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>("Login", "some_username") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
                { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}.") },
            };
            var formatter = new FormattedLogValuesFormatter(list, null, null);
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorOriginalPropertyListFormatter()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>("Login", "some_username") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
            };
            var formatter = new FormattedLogValuesFormatter(list, () => "Invoked default formatter", null);
            Assert.AreEqual("Invoked default formatter", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatString()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void ConstructorNamedFormatStringFormatException()
        {
            _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter(null, "Login: {{Login}. Password: {Password}.", "some_username", "some_password"));
        }
        [TestMethod]
        public void IndexerGetValue()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));

            Assert.AreEqual("some_username", formatter[0].Value);
            Assert.AreEqual("[Redacted]", formatter[1].Value);
            Assert.AreEqual("Login: {Login}. Password: {Password}.", formatter[2].Value);

            Assert.AreEqual("some_username", formatter["Login"]);
            Assert.AreEqual("[Redacted]", formatter["Password"]);
            Assert.AreEqual("Login: {Login}. Password: {Password}.", formatter[FormattedLogValuesFormatter.OriginalFormat]);

            _ = Assert.ThrowsException<IndexOutOfRangeException>(() => formatter[3]);
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter[null!]);
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter["InvalidKey"]);
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
        public void FormatProviderInvariantCulture()
        {
            var dateTime = DateTime.UtcNow;
            var formatter = new FormattedLogValuesFormatter(null, "DateTime: {DateTime}. Login: {Login}. Password: {Password}.", dateTime, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {dateTime.ToString(CultureInfo.InvariantCulture)}. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void FormatProviderSpecifiedCulture()
        {
            var dateTime = DateTime.UtcNow;
            var formatProvider = new CultureInfo("uk-ua");
            var formatter = new FormattedLogValuesFormatter(formatProvider, "DateTime: {DateTime}. Login: {Login}. Password: {Password}.", dateTime, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: {dateTime.ToString(formatProvider)}. Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void NullParameterValue()
        {
            var formatter = new FormattedLogValuesFormatter(null, "DateTime: {DateTime}. Login: {Login}. Password: {Password}.", null, "some_username", "some_password");
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual($"DateTime: (null). Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void NullParameterEnumerable()
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