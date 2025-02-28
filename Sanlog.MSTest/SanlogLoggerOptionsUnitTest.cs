namespace Sanlog.MSTest
{
    [TestClass]
    public sealed class SanlogLoggerOptionsUnitTest
    {
        [TestMethod]
        public void ParameterlessConstructor() => Assert.IsNotNull(typeof(SanlogLoggerOptions).GetConstructor(Type.EmptyTypes));
        [TestMethod]
        public void OnRetrieveVersion()
        {
            var options = new SanlogLoggerOptions();
            Assert.IsNotNull(options.OnRetrieveVersion);
            var version = options.OnRetrieveVersion.Invoke();
            Assert.IsNotNull(version);
            Assert.AreEqual(typeof(SanlogLoggerOptionsUnitTest).Assembly.GetName().Version, version);
        }
    }
}