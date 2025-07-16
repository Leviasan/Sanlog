# Sanlog.Abstractions

Provides a base infrastructure of the logger that supports saving log entries from different applications in one storage, separated by an app and tenant identifiers.
ATTENTION! Only for apps that use a host.

## Install the package

From the command-line:

```console
dotnet add package Sanlog.Abstractions
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Sanlog.Abstractions" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Usage Example

### Implementing `SanlogLoggerProvider`

```csharp
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[ProviderAlias(nameof(SanlogLoggerProvider))]
internal sealed class MyCustomSanlogLoggerProvider : SanlogLoggerProvider
{
    public MyCustomSanlogLoggerProvider(IMessageReceiver receiver, IRedactorProvider redactorProvider, IOptions<SanlogLoggerOptions> options)
        : base(receiver, redactorProvider, options) { }
    public MyCustomSanlogLoggerProvider(IMessageReceiver receiver, IOptions<SanlogLoggerOptions> options)
        : base(receiver, options) { }
}
```

### Implementing `IMessageHandler`

```csharp
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

internal sealed class LoggingEntryMessageHandler : IMessageHandler
{
    public async ValueTask HandleAsync(object? message, CancellationToken cancellationToken)
    {
        if (message is LoggingEntry loggingEntry)
        {
            // TODO: Save to the storage
        }
    }
}
```

### Implementing extension method `ILoggingBuilder`

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddSanlog(
        this ILoggingBuilder builder,
        Action<SanlogLoggerOptions>? loggingConfigure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // TODO: Register new services which your new 'SanlogLoggerProvider' or/and 'IMessageHandler' depends on

        builder.AddConfiguration();
        builder.Services
            .AddSanlogInfrastructure(builder => builder.SetHandler<MyCustomSanlogLoggerProvider, LoggingEntryMessageHandler>())
            .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, MyCustomSanlogLoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, MyCustomSanlogLoggerProvider>(builder.Services);
        if (loggingConfigure is not null)
            _ = builder.Services.Configure(loggingConfigure);
        return builder;
    }
}
```

## Formatting

### `record class` or `record struct`
- parameter without @ParamName = ignored `DataClassificationAttribute` and invoke `Convert.ToString`
- use `[property: DataClassificationAttribute]` to get and use redactor

## Feedback & Contributing
Welcome feedback and contributions in [our GitHub repo](https://github.com/Leviasan/Sanlog).

## Miscellaneous
- Channels (https://learn.microsoft.com/en-us/dotnet/core/extensions/channels).
- Multi-tenancy (https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy).
- Define and Use Custom Format Providers (https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-define-and-use-custom-numeric-format-providers).