# Sanlog.Abstractions

Provides a base infrastructure of the logger that supports saving log entries from different applications in one storage, separated by an app and tenant identifiers.

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

### Implementing custom `SanlogLoggerProvider`
It's an example of how to implement your custom provider.

```csharp
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanlog.Brokers;

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
It's an example of how to implement business logic to save your message to storage.

```csharp
using System.Threading;
using System.Threading.Tasks;
using Sanlog.Brokers;

internal sealed class MyCustomLoggingEntryMessageHandler : IMessageHandler
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
It's an example of how to implement a helper method to register a logger to DI.

```csharp
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sanlog.Brokers;

public static class ILoggingBuilderExtensions
{
    public static ILoggingBuilder AddSanlogMyCustomProvider(
        this ILoggingBuilder builder,
        Action<DbContextOptionsBuilder> contextConfigure,
        Action<SanlogLoggerOptions>? loggingConfigure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(contextConfigure);

        builder.AddConfiguration();
        builder.Services
            .AddMessageBroker(builder => builder.SetHandler<MyCustomSanlogLoggerProvider, MyCustomLoggingEntryMessageHandler>()) // register here your IMessageHandler
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
- During serialization parameter with @ParamName => ignored `DataClassificationAttribute` and invoke `Convert.ToString`. To prevent this case need to use `[property: DataClassificationAttribute]` while defining a property.

## Feedback & Contributing
Welcome feedback and contributions in [our GitHub repo](https://github.com/Leviasan/Sanlog).

## Miscellaneous
- [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)
- [Multi-tenancy](https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy)
- [Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line)
- [Define and Use Custom Format Providers](https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-define-and-use-custom-numeric-format-providers)