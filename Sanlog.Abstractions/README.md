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

### Implementing `ILoggerProvider`

Logger provider can be implemented by inheriting from `Sanlog.SanlogLoggerProvider`. For example:

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sanlog.Brokers;
using Sanlog.Formatters;


[ProviderAlias(nameof(SanlogLoggerProvider))]
internal sealed class EntityFrameworkCoreSanlogLoggerProvider(IMessageReceiver receiver, FormattedLogValuesFormatter formatter, IOptions<SanlogLoggerOptions> options)
    : SanlogLoggerProvider(receiver, formatter, options) { }
```

### Implementing Redactor Providers

Redactor Providers implement `Microsoft.Extensions.Compliance.Redaction.IRedactorProvider`.
For example:

```csharp
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

public sealed class StarRedactorProvider : IRedactorProvider
{
    private static readonly StarRedactor _starRedactor = new();

    public static StarRedactorProvider Instance { get; } = new();

    public Redactor GetRedactor(DataClassificationSet classifications) => _starRedactor;
}
```

## Feedback & Contributing

Welcome feedback and contributions in [our GitHub repo](https://github.com/Leviasan/Sanlog).

## Miscellaneous
- Channels (https://learn.microsoft.com/en-us/dotnet/core/extensions/channels).
- Multi-tenancy (https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy).
- Define and Use Custom Format Providers (https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-define-and-use-custom-numeric-format-providers).