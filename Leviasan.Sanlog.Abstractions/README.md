# Sanlog.Abstractions
Represents the logger for the NET8.0+ apps. API provides a logger that supports saving log entries from different applications in one storage place separated by an app identifier that supports multi-tenancy.

### Helper classes
- `MessageTemplate` - Represents a message template.
- `SensitiveFormatter` - Represents the formatter that supports custom formatting of objects considering redact sensitive data.
- `FormattedLogValuesFormatter` - Represents the formatter that supports Microsoft custom of the formatting.Extensions.Logging.FormattedLogValues objects. It depends on `MessageTemplate` and is inherited from `SensitiveFormatter`.