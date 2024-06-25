using System.Globalization;

namespace Leviasan.Sanlog.MSTest
{
    [TestClass]
    public sealed class FormattedLogValuesFormatterUnitTest
    {
        private static readonly DateTime DateTimeValue = new(2024, 5, 22, 23, 56, 18);
        private static readonly DateTimeOffset DateTimeOffsetValue = new(DateTimeValue);

        [TestMethod]
        public void CtorListOrdered()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>("Login", "some_username") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
                { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}.") },
            };
            var formatter = new FormattedLogValuesFormatter(list, null);
            Assert.IsTrue(formatter.HasOriginalFormat);

            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        public void CtorListOrderedNo()
        {
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>(FormattedLogValuesFormatter.OriginalFormat, "Login: {Login}. Password: {Password}.") },
                { KeyValuePair.Create<string, object?>("Password", "some_password") },
                { KeyValuePair.Create<string, object?>("Login", "some_username") }
            };
            var formatter = new FormattedLogValuesFormatter(list, null);
            Assert.IsTrue(formatter.HasOriginalFormat);

            Assert.AreEqual("Login: some_username. Password: some_password.", formatter.ToString());
            Assert.AreEqual(1, formatter.RegisterSensitiveData([FormattedLogValuesFormatter.OriginalFormat, "Password"]));
            Assert.IsFalse(formatter.IsSensitiveData(FormattedLogValuesFormatter.OriginalFormat));
            Assert.IsTrue(formatter.IsSensitiveData("Password"));
            Assert.AreEqual("Login: some_username. Password: [Redacted].", formatter.ToString());
        }
        [TestMethod]
        [DataRow("")]
        [DataRow("uk-ua")]
        public void CtorListProviderDependence(string culture)
        {
            IFormatProvider? formatProvider = CultureInfo.GetCultureInfo(culture);
            var list = new List<KeyValuePair<string, object?>>
            {
                { KeyValuePair.Create<string, object?>("Single", MathF.PI) },
                { KeyValuePair.Create<string, object?>("Double", Math.PI) },
                { KeyValuePair.Create<string, object?>("Enum", ConsoleColor.Red) },
                { KeyValuePair.Create<string, object?>("DateTime", DateTimeValue) },
                { KeyValuePair.Create<string, object?>("DateTimeOffset", DateTimeOffsetValue) },
                { KeyValuePair.Create<string, object?>("NullValue", null) },
                { KeyValuePair.Create<string, object?>("Enumerable", new object?[3] { 1, null, 2 }) },
                { KeyValuePair.Create<string, object?>("Dictionary", new Dictionary<string, object?> { { "NotNull", 1 }, { "NullValue", null } }) }
            };
            var formatter = new FormattedLogValuesFormatter(list, formatProvider);
            Assert.IsFalse(formatter.HasOriginalFormat);

            Assert.AreEqual(MathF.PI.ToString("G9", formatProvider), formatter["Single"].Value);
            Assert.AreEqual(Math.PI.ToString("G17", formatProvider), formatter["Double"].Value);
            Assert.AreEqual(ConsoleColor.Red.ToString("D"), formatter["Enum"].Value);
            Assert.AreEqual(DateTimeValue.ToString("O", formatProvider), formatter["DateTime"].Value);
            Assert.AreEqual(DateTimeOffsetValue.ToString("O", formatProvider), formatter["DateTimeOffset"].Value);
            Assert.AreEqual("(null)", formatter["NullValue"].Value);
            Assert.AreEqual("[1, (null), 2]", formatter["Enumerable"].Value);
            Assert.AreEqual("[[NotNull, 1], [NullValue, (null)]]", formatter["Dictionary"].Value);
        }
        [TestMethod]
        public void CtorNamedFormatUseInt32KeyName()
        {
            var formatter = new FormattedLogValuesFormatter(formatProvider: null, format: string.Empty, args: "some_username");
            Assert.IsFalse(formatter.HasOriginalFormat);

            Assert.AreEqual("0", formatter[0].Key);
            Assert.AreEqual("some_username", formatter[0].Value);
        }
        [TestMethod]
        public void CtorNamedFormatException()
        {
            _ = Assert.ThrowsException<FormatException>(() => new FormattedLogValuesFormatter(null, "Login: {{Login}. Password: {Password}.", "some_username", "some_password"));
        }
        [TestMethod]
        public void GetObjectAsString()
        {
            var formatter = new FormattedLogValuesFormatter(null, "Login: {Login}. Password: {Password}.", "some_username", "some_password");
            Assert.IsTrue(formatter.HasOriginalFormat);

            Assert.IsTrue(formatter.RegisterSensitiveData("Password"));
            Assert.AreEqual("[Redacted]", formatter.GetObjectAsString(1, true).Value);
            Assert.AreEqual("some_password", formatter.GetObjectAsString(1, false).Value);
            Assert.AreEqual("[Redacted]", formatter.GetObjectAsString("Password", true).Value);
            Assert.AreEqual("some_password", formatter.GetObjectAsString("Password", false).Value);

            _ = Assert.ThrowsException<ArgumentOutOfRangeException>(() => formatter.GetObjectAsString(3, true));
            _ = Assert.ThrowsException<ArgumentNullException>(() => formatter.GetObjectAsString(null!, true));
            _ = Assert.ThrowsException<KeyNotFoundException>(() => formatter.GetObjectAsString("InvalidKey", true));
        }
    }
}