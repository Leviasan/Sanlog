using System.Data;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Sanlog.Formatters;

namespace Sanlog.Abstractions.MSTest
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var options = new SanlogLoggerOptions();
            var formatter = new FormattedLogValuesFormatter(new RedactorProvider(), options.FormattedOptions ?? LoggerFormatterOptions.Default);
            var logValues = new FormattedLogValues(formatter, new Dictionary<string, object?>
            {
                { "Key1", CommandType.Text },
                { "Key2", null },
            });

            // "CommandType: (null). Parameters: [[Key1, (null)], [Key2, 15]]"
        }

        private sealed class RedactorProvider : IRedactorProvider
        {
            public Redactor GetRedactor(DataClassificationSet classifications) => NullRedactor.Instance;
        }
    }
}
