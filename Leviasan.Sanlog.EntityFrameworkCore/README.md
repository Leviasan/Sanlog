# Leviasan.Sanlog.EntityFrameworkCore
Represents the logger for the NET8.0+ apps. Provides API for using EntityFrameworkCore context as storage for saving log entries from different applications in one database scheme separated by application identifier.

# Get started

## Add a logger to pipeline
There are 2 extension methods to add a logger to service collection. The first approach writes an event to the database after it occurred.
```csharp
public static ILoggingBuilder AddSanlogService(this ILoggingBuilder builder, ...)
```
This approach uses `BackgroundService` to write an event to the queue, that writes one to the database in another thread.
```csharp
public static ILoggingBuilder AddHostedSanlogService(this ILoggingBuilder builder, ...)
```

## Migration and implementation of the IDesignTimeDbContextFactory<SanlogDbContext>
Install an additional NuGet package `Microsoft.EntityFrameworkCore.Tools`.
To create a migration need to invoke `Add-Migration InitialDatabase -Context SanlogDbContext`. To apply need to invoke `Update-Database -Context SanlogDbContext`.
Simple implementation of the `IDesignTimeDbContextFactory<SanlogDbContext>` for a trivial app that uses `Microsoft.EntityFrameworkCore.SqlServer` as log entry storage.
```csharp
internal sealed class SanlogDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SanlogDbContext>
{
    public SanlogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SanlogDbContext>();
        // Here need Use* method of your database provider
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True";
        optionsBuilder.UseSqlServer(connectionString, serverOptions =>
        {
            // [Required] Set migration assembly
            serverOptions.MigrationsAssembly(typeof(SanlogDbContextDesignTimeDbContextFactory).Assembly.GetName().Name);
        });
        return new SanlogDbContext(optionsBuilder.Options);
    }
}
```
Design-time service for an application that used many environments.
To create a migration need to invoke `Add-Migration InitialDatabase -Args "appsettings.json LoggerConnection" -Context SanlogDbContext`. To apply need to invoke `Update-Database -Args "appsettings.json LoggerConnection" -Context SanlogDbContext`.
```csharp
internal sealed class SanlogDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SanlogDbContext>
{
    public SanlogDbContext CreateDbContext(string[] args)
    {
        if (args.Length != 2)
            throw new InvalidOperationException("Invalid arguments count");
        
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
        configurationBuilder.AddJsonFile(args[0]);
        var configuration = configurationBuilder.Build()!;
        
        var optionsBuilder = new DbContextOptionsBuilder<SanlogDbContext>();
        var connectionString = configuration.GetConnectionString(args[1]);
        optionsBuilder.UseSqlServer(connectionString, serverOptions => // Here need Use* method of your database provider
        {
            // [Required] Set migration assembly
            serverOptions.MigrationsAssembly(typeof(SanlogDbContextDesignTimeDbContextFactory).Assembly.GetName().Name);
        });
        return new SanlogDbContext(optionsBuilder.Options);
    }
}
```

## Registers your application
You must connect to your database provider and invoke the `insert` command to the `LogApps` table. For example for MSSQL: `insert into dbo.LogApps values ('e6bcc7df-e201-4d0b-02a3-08dbd09ffc89', 'ConsoleApp', 'Development')`.

## Logging in a trivial app
This first example shows the basics, but it is only suitable for a trivial console app. In the next section, you see how to improve the code by considering scale, performance, configuration, and typical programming patterns.
Example for database provider: `Microsoft.EntityFrameworkCore.SqlServer`.
```csharp
internal sealed partial class Program
{
    private static void Main(string[] args)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSanlogService(
            contextConfigure =>
            {
                // Configure your database provider
                var connectionString = "Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True";
                contextConfigure.UseSqlServer(connectionString);
            },
            loggingConfigure =>
            {
                // [Required] Here need to insert your specific application identifier
                loggingConfigure.AppId = Guid.Parse("e6bcc7df-e201-4d0b-02a3-08dbd09ffc89");
            }));
        ILogger logger = factory.CreateLogger(nameof(Program));
        LogInvokedMethod(logger, null, nameof(Program), nameof(Main));
        Console.WriteLine("Finished");
    }
    [LoggerMessage(Level = LogLevel.Information, Message = "ClassName: {ClassName}. Method: {MethodName}")]
    static partial void LogInvokedMethod(ILogger logger, Exception? exception, string className, string methodName);
}
```
The preceding example:
- Creates an `ILoggerFactory`. The `ILoggerFactory` stores all the configuration and determines where log messages are sent.
- Creates an `ILogger` with a category named "Program". The category is a string associated with each message logged by the `ILogger` object. It's used to group log messages from the same class (or category) together while searching or filtering logs.
- Calls `LogInvokedMethod` to log a message at the Information level. The log level indicates the logged event's severity and filters out less important log messages. The log entry also includes a message template "ClassName: {ClassName}. Method: {MethodName}" and a key-value pairs ClassName = Program and MethodName = Main. The key name (or placeholder) comes from the word inside the curly braces in the template and the value comes from the remaining method argument. Logging compile-time source generation is usually a better alternative to `ILogger` extension methods like `LogInformation`. Logging source generation offers better performance, and stronger typing, and avoids spreading string constants throughout your methods. The tradeoff is that using this technique requires a bit more code.

## Integration with hosts and dependency injection
If your application uses Dependency Injection (DI) or a host such as ASP.NET's WebApplication or Generic Host then you should use `ILoggerFactory` and `ILogger` objects from the DI container rather than creating them directly. Host builders initialize the [default configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host), then add a configured `ILoggerFactory` object to the host's DI container when the host is built. Before the host is built you can adjust the logging configuration via `HostApplicationBuilder.Logging`, `WebApplicationBuilder.Logging`, or similar APIs on other hosts. Hosts also apply logging configuration from default configuration sources such as `appsettings.json` and environment variables. For more information, see [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration).
```csharp
internal sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        /* ... Another configuration ... */
        builder.AddHostedSanlogService(contextConfigure =>
        {
            // Configure your database provider
            var connectionString = builder.Configuration.GetConnectionString("LoggerConnection");
            contextConfigure.UseSqlServer(connectionString);
        });
        var app = builder.Build();
        /* ... Configuring pipeline ... */
        app.Run();
    }
}
```

## Configure logging
Logging configuration is set in code or via external sources, such as config files and environment variables. Using external configuration is beneficial when possible because it can be changed without rebuilding the application. However, some tasks, such as setting logging providers, can only be configured from code.
For apps that use a host, logging configuration is commonly provided by the "Logging" section of `appsettings.{Environment}.json` files. For apps that don't use a host, external configuration sources are set up explicitly or configured in code instead.
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "SanlogLoggerProvider": {
      "AppId": "e6bcc7df-e201-4d0b-02a3-08dbd09ffc89", // [Required] Here need to insert your specific application identifier
      "IncludeScopes": true,
      "CultureName": "uk-ua", // [Optional] Override default format provider CultureInfo.InvariantCulture
      "LogLevel": { // [Optional] Override default log level configuration
        "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Information"
      }
    }
  }
}
```

# Attention!
- `ODP.NET` does support `Guid`s. `Guid`s can be inserted into a `RAW(16)` column which is big enough to hold any `Guid` value. But caution needs to be taken in order to handle `Guid`s appropriately. This is due to the fact that as the .NET `Guid` structure flips the byte values in reverse order for the integer-based parts of the `Guid` values when `Guid(byte[ ])` constructor is used and when the `ToByteArray()` method on the `Guid` struct is invoked. More information: https://docs.oracle.com/en/database/oracle/oracle-database/23/odpnt/featGUID.html.
- If using `EFCore.NamingConventions` you must configure `DbContextOptionsBuilder` using invoke `Use*CaseNamingConvention` in both places with the same method. It is the design time service that implements `IDesignTimeDbContextFactory<SanlogDbContext>` and the second place is while configuring the Sanlog logger.