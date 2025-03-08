using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanlog.Brokers;
using Sanlog.Formatters;

namespace Sanlog
{
    [ProviderAlias(nameof(SanlogLoggerProvider))]
    internal sealed class EntityFrameworkCoreSanlogLoggerProvider(IMessageReceiver receiver, FormattedLogValuesFormatter formatter, IOptions<SanlogLoggerOptions> options)
        : SanlogLoggerProvider(receiver, formatter, options)
    { }
}