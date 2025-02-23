# Sanlog
Provides a logger that supports saving log entries from different applications in one storage, separated by an app and tenant identifiers.

#### SensitiveFormatter support serialize operator '@'
```csharp
private sealed record class Position(int Latitude, int Longitude);

var formatter = new FormattedLogValuesFormatter("Processed {@Position} in {Elapsed:000} ms.", new Position(25, 134), 34);
Assert.AreEqual("Processed { Latitude = 25, Longitude = 134 } in 034 ms.", formatter.ToString());
```

### Miscellaneous
- Multi-tenancy (https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy).
- Define and Use Custom Format Providers (https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-define-and-use-custom-numeric-format-providers).