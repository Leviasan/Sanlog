using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sanlog.Formatters;

namespace Sanlog.Abstractions.MSTestProject
{
    [TestClass]
    public sealed class FormattedLogValuesFormatterTest
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            // This method is called once for the test assembly, before any tests are run.
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            // This method is called once for the test assembly, after all tests are run.
        }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            // This method is called once for the test class, before any tests of the class are run.
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // This method is called once for the test class, after all tests of the class are run.
        }

        [TestInitialize]
        public void TestInit()
        {
            // This method is called before each test method.
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // This method is called after each test method.
        }

        [TestMethod]
        public void ByteArrayFormatter()
        {
            Dictionary<string, object?> parameters = new()
            {
                { "filedata", new byte[3] { 1,2,3 } }
            };
            FormattedLogValuesFormatter formatter = new(NullRedactorProvider.Instance, LoggerFormatterOptions.Default);
            FormattedLogValues logValues = new(formatter, "Parameters: {@Parameters}", parameters);
            string? actual = logValues.ToString();
            Assert.AreEqual("Parameters: [[filedata, [*System.Byte[3]*]]]", actual);
        }
    }
}