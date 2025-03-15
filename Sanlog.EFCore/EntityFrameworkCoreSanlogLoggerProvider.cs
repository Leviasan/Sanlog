using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sanlog.EntityFrameworkCore
{
    [ProviderAlias(nameof(SanlogLoggerProvider))]
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class EntityFrameworkCoreSanlogLoggerProvider(IMessageReceiver receiver, IRedactorProvider redactorProvider, IOptions<SanlogLoggerOptions> options)
        : SanlogLoggerProvider(receiver, redactorProvider, options)
    { }
}