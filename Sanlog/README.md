# Sanlog
Provides a logger that supports saving log entries from different applications in one storage, separated by an app and tenant identifiers.

### Helper classes
- `MessageTemplate` - Represents a composite/named message template.
- `SensitiveFormatter` - Represents a formatter that supports the concealment of confidential data. It depends on `SensitiveConfiguration` and `FormatItemType`.
- `FormattedLogValuesFormatter` - Represents the formatter that supports custom formatting of `Microsoft.Extensions.Logging.FormattedLogValues` object. It depends on `MessageTemplate` and is inherited from `SensitiveFormatter`.

### Miscellaneous
- Multi-tenancy (https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy).