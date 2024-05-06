# Changelog

### 1.1.0
- Added mechanism to format message and property values before logging `FormattedLogValuesFormatter`. Support globalization through `SanlogLoggerOptions.CultureName`.
- `SanlogLoggerOptions` added a new method `RegisterSensitiveData` instead of the property `IgnorePropertyKeys`. The value of the registered property will be redacted before logging.
- `SanlogLoggerOptions.JsonSerializerOptions`, `LoggingError.Data` and `LoggingError.Properties` are obsolete. NativeAOT does not support reflection-based serialization.
- `SanlogLoggerOptions.SuppressThrowing` is obsolete. Logging entry writer services must handle their exception.
- `SanlogLoggerOptions.SkipEnabledCheck` is obsolete. `ILogger.IsEnabled` is always invoked.