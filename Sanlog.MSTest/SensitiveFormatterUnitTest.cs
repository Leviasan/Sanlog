namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class SensitiveFormatterUnitTest
    {
        [TestMethod]
        public void AllowNullOptions()
        {
            var configuration = new SensitiveFormatterOptions();
            Assert.IsTrue(configuration.AddSensitive(SensitiveKeyType.SegmentName, "Password"));
            var formatter = new SensitiveFormatter()
            {
                SensitiveConfiguration = configuration
            };
            Assert.IsNull(formatter.CultureInfo);
            Assert.AreEqual(configuration, formatter.SensitiveConfiguration);
            formatter.SensitiveConfiguration = null; // reset to default
            Assert.AreNotEqual(configuration, formatter.SensitiveConfiguration);
        }
    }
}