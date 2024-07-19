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
- `FormattedLogValuesFormatter` uses `CultureInfo.CurrentCulture` by default if the `formatProvider` is passed to the constructor as `null`.
- Added method `FormattedLogValuesFormatter.GetData` to get an original or redacted string representation of the object.
- Fixed invalid argument value passed to the `string.format` method if the `FormattedLogValuesFormatter.ToString` method invoked twice before and after invoking method `FormattedLogValuesFormatter.RegisterSensitiveData`.

### 1.1.2
- MSBuild property `GenerateDocumentationFile` is disabled.
- Fixed behavior while formatting `IDictionary` and `IEnumerable` elements to a string representation in the same format as the other value.
- `FormattedLogValuesFormatter` for types `DateTime` and `DateTimeOffset` use a round-trip date/time pattern ("O") defined in ISO 8601 to format to a string representation.
- `FormattedLogValuesFormatter` for type `Enum` uses a decimal pattern ("D") to display the enumeration entry as an integer value in the shortest representation possible.
- Renamed method `FormattedLogValuesFormatter.GetData` to `FormattedLogValuesFormatter.GetObjectAsString` to get an original or redacted string representation of the object.
- Added method `FormattedLogValuesFormatter.GetObject` to get an original raw or redacted value.

### 1.1.3
- `FormattedLogValuesFormatter` formats `IEnumerable` objects as: [object, object2, ..., objectN].
- `FormattedLogValuesFormatter` formats `IDictionary` objects as: [[Key1, Value1], [Key2, Value2]].
- `FormattedLogValuesFormatter` for type `Single` uses the "G9" format specifier to ensure that the original `Single` value successfully round-trips (IEEE 754-2008-compliant).
- `FormattedLogValuesFormatter` for type `Double` uses the "G17" format specifier to ensure that the original `Double` value successfully round-trips (IEEE 754-2008-compliant).

### 2.0.0
- MSBuild property `GenerateDocumentationFile` is enabled.
- `FormattedLogValuesFormatter` implements `IFormatProvider` and `ICustomFormatter`.
- `FormattedLogValuesFormatter` and `SanlogLoggerOptions` provide new API `RegisterSensitiveData(Type, string)` to register property whose value belongs to sensitive data. Using `string` redacts the property of the composite format string and `DictionaryEntry` redacts the dictionary entry value.
- `NamedFormatString` renamed to `MessageTemplate`.