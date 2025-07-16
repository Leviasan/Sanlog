using System.Data;
using Microsoft.Extensions.Compliance.Redaction;
using Sanlog.Formatters;

namespace Sanlog.Abstractions.MSTest
{
    [TestClass]
    public sealed class FormattedLogValuesTests
    {
        [TestMethod]
        public void SerializeObjectEquivalentKey()
        {
            var options = new SanlogLoggerOptions();
            var formatter = new FormattedLogValuesFormatter(NullRedactorProvider.Instance, options.FormattedOptions ?? LoggerFormatterOptions.Default);
            var logValues = new FormattedLogValues(formatter, new Dictionary<string, object?>
            {
                { "CommandType", CommandType.Text },
                { "Parameters", new Dictionary<string, object?> { { "Key1", null }, { "Key2", 15 } } },
                { "{OriginalFormat}", "CommandType: {CommandType:G}. Parameters: {@Parameters}" }
            });
            Assert.AreEqual("CommandType: Text. Parameters: [[Key1, (null)], [Key2, 15]]", logValues.ToString());
        }
    }
}