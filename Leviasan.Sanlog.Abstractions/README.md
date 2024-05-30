# Leviasan.Sanlog.Abstractions
Represents the logger for the NET8.0+ apps. Provides a logger that supports saving log entries from different applications in one storage place separated by app identifier.

# Helper independent classes
- `NamedFormatString` - Represents a parsed named format string.
- `FormattedLogValuesFormatter` - Represents the formatter of the Microsoft.Extensions.Logging.FormattedLogValues class. Depends on `NamedFormatString`.