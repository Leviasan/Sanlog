# Changelog

### 1.1.0
- Added mechanism to format message and property values before logging `FormattedLogValuesFormatter`. Support globalization through `SanlogLoggerOptions.CultureName`.
- `SanlogLoggerOptions` added a new method `RegisterSensitiveData` instead of the property `IgnorePropertyKeys`. The value of the registered property will be redacted before logging.
- `SanlogLoggerOptions.JsonSerializerOptions`, `LoggingError.Data` and `LoggingError.Properties` are obsolete. NativeAOT does not support reflection-based serialization.
- `SanlogLoggerOptions.SuppressThrowing` is obsolete. Logging entry writer services must handle their exception.
- `SanlogLoggerOptions.SkipEnabledCheck` is obsolete. `ILogger.IsEnabled` is always invoked.

### 1.1.1
- `SanlogLoggerOptions.CultureName` is obsolete. `SanlogLogger` using `CultureInfo.InvariantCulture` as a format provider to format arguments.
- `FormattedLogValuesFormatter` for types `DateTime` and `DateTimeOffset` use a sortable date/time pattern ("s") defined in ISO 8601 to format to the string representation.
- `FormattedLogValuesFormatter` uses `CultureInfo.CurrentCulture` by default if the `formatProvider` is passed to the constructor as null.
- Added method `FormattedLogValuesFormatter.GetData` to get an original or redacted string representation of the object.
- Fixed invalid argument value passed to the `string.format` method if the `FormattedLogValuesFormatter.ToString` method invoked twice before and after invoking method `FormattedLogValuesFormatter.RegisterSensitiveData`.

### 1.1.2
- MSBuild property `GenerateDocumentationFile` is disabled.
- Fixed behavior while formatting `IDictionary` and `IEnumerable` elements to a string representation in the same format as the other value.
- `FormattedLogValuesFormatter` for types `DateTime` and `DateTimeOffset` use a round-trip date/time pattern ("O") defined in ISO 8601 to format to a string representation.
- `FormattedLogValuesFormatter` for type `Ehnum` uses a decimal pattern ("D") to display the enumeration entry as an integer value in the shortest representation possible.
- Renamed method `FormattedLogValuesFormatter.GetData` to `FormattedLogValuesFormatter.GetObjectAsString` to get an original or redacted string representation of the object.
- Added method `FormattedLogValuesFormatter.GetObject` to get an original raw or redacted value.