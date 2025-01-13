# Sanlog
Provides a logger that supports saving log entries from different applications in one storage, separated by an app and tenant identifiers.

### Helper classes
- `MessageTemplate` - Represents a composite/named message template.
- `SensitiveFormatter` - Represents a formatter that supports the concealment of confidential data. It depends on `SensitiveFormatterOptions`.
- `FormattedLogValuesFormatter` - Represents the formatter that supports custom formatting of `Microsoft.Extensions.Logging.FormattedLogValues` object. It depends on `MessageTemplate`, `FormattedLogValuesFormatterOptions`, and is inherited from `SensitiveFormatter`.
- `ChannelMessageBroker` - Represents a service to send/deliver messages to handlers based on `Channel`. It depends on `IMessageBroker` and `IMessageHandler`.

#### SensitiveFormatter support serialize operator '@'
```csharp
private sealed record class Position(int Latitude, int Longitude);

var formatter = new FormattedLogValuesFormatter("Processed {@Position} in {Elapsed:000} ms.", new Position(25, 134), 34);
Assert.AreEqual("Processed { Latitude = 25, Longitude = 134 } in 034 ms.", formatter.ToString());
```

### Miscellaneous
- Multi-tenancy (https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy).