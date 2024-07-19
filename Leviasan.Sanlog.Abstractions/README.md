# Leviasan.Sanlog.Abstractions
Represents the logger for the NET8.0+ apps. Provides a logger that supports saving log entries from different applications in one storage place separated by app identifier.

### Independent public classes
- `MessageTemplate` - Represents a message template.
- `FormattedLogValuesFormatter` - Represents the formatter that supports custom formatting of Microsoft.Extensions.Logging.FormattedLogValues object. Depends on `MessageTemplate`.