# Changelog

### 1.1.0
- Changed method names `ILoggingBuilder.AddSanlogService` and `ILoggingBuilder.AddHostedSanlogService` to add the logger to the service collection.
- `LoggingEntry.Message` and `LoggingEntryProperty.Value` and `LoggingScope.Message` and `LoggingScopeProperty.Value` have unlimited string length.
- Updated NuGet dependency `Microsoft.EntityFrameworkCore` from 8.0.2 to 8.0.4.

### 1.1.1
- `LoggingEntryProperty.Id`, `LoggingScope.Id` and `LoggingScopeProperty.Id` never have a value generated when an instance of this entity type is saved.
- Updated NuGet dependency `Microsoft.EntityFrameworkCore` from 8.0.4 to 8.0.5.

### 1.1.2
- MSBuild property `GenerateDocumentationFile` is disabled.
- Updated NuGet dependency `Microsoft.EntityFrameworkCore` from 8.0.5 to 8.0.6.