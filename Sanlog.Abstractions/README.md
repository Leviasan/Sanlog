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

## Formatters

There are 2 approaches to override format string:

1. Invoke the `OverrideFormat` method in the `SanlogLoggerOptions.FormattedOptions` to define a custom format for your object.
```csharp
using Sanlog;

SanlogLoggerOptions options = new();
options.FormattedOptions.OverrideFormat<float>("G9"); // override for float use G9 format
```

2. Invoke the `RegisterFormatter` method in the `SanlogLoggerOptions.FormattedOptions` to define a custom `IValueFormatter` format for your object.
```csharp
using Sanlog.Formatters;

public sealed class ByteArrayFormatter : IValueFormatter
{
    public const string FormatRedacted = "R";

    public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        if (Equals(formatProvider) && arg is byte[] bytes)
        {
            if (string.IsNullOrEmpty(format))
            {
                return bytes.ToString()!;
            }
            else if (format.Equals(FormatRedacted, StringComparison.Ordinal))
            {
                return $"[{typeof(byte).FullName}[{bytes.Length}]]";
            }
            throw new FormatException(string.Format(CultureInfo.InvariantCulture, "'{0}' cannot be used to format {1}.", format, arg.GetType()));
        }
        return DefaultFallback(format, arg, Equals(formatProvider) ? null : formatProvider);

        static string DefaultFallback(string? format, object? arg, IFormatProvider? formatProvider)
        {
            return arg switch
            {
                IFormattable formattable => formattable.ToString(format, formatProvider),
                _ => Convert.ToString(arg, formatProvider) ?? string.Empty
            };
        };
    }
}

SanlogLoggerOptions options = new();
options.FormattedOptions.RegisterFormatter<byte[]>(new ByteArrayFormatter(), ByteArrayFormatter.FormatRedacted)); // override for byte[] use custom R format
```

## Attention!
- During object serialization, which is represented as `record class` or `record struct` with declared properties in the constructor and specified as @ParamName 
=> ignored `DataClassificationAttribute`, and invoke `Convert.ToString`. To prevent this case, use `[property: DataClassificationAttribute]` while defining a property.

## Feedback & Contributing
Welcome feedback and contributions in [our GitHub repo](https://github.com/Leviasan/Sanlog).

## Miscellaneous
- [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels)
- [Multi-tenancy](https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy)
- [Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line)
- [Define and Use Custom Format Providers](https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-define-and-use-custom-numeric-format-providers)