using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanlog.Brokers;
using Sanlog.Formatters;

namespace Sanlog
{
    [ProviderAlias(nameof(SanlogLoggerProvider))]
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class EntityFrameworkCoreSanlogLoggerProvider(IMessageReceiver receiver, FormattedLogValuesFormatter formatter, IOptions<SanlogLoggerOptions> options)
        : SanlogLoggerProvider(receiver, formatter, options)
    { }
}