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
        [TestMethod]
        public void CustomFormatter()
        {
            var position = new Position(25, 134);
            var formatter = new SensitiveFormatter();
            Assert.AreEqual("{ Latitude = 25, Longitude = 134 }", formatter.Format("P", position, formatter));
            Assert.AreEqual("{ Latitude = 25, Longitude = 134 }", string.Format(formatter, "{0:P}", position));
            Assert.AreEqual(position.ToString(), string.Format(formatter, "{0}", position));
        }
    }
}