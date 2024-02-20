# Leviasan.Sanlog.EntityFrameworkCore
Represents a logger provider that uses an EntityFrameworkCore context as storage for saving log entries from different applications in one database scheme separated by application identifier.

## Get started
There are 2 extension methods to add a logger. The first approach writes events to the storage in a synchronous mode.
```csharp
public static ILoggingBuilder AddSanlogEntityFrameworkCore(this ILoggingBuilder builder, ...)
```
This approach uses `BackgroundService` to write events the storage in an asynchronous mode. If possible, you should use this API.
```csharp
public static IHostApplicationBuilder AddSanlogEntityFrameworkCore(this IHostApplicationBuilder builder, ...)
```
For creating a database needs to install an additional NuGet package `Microsoft.EntityFrameworkCore.Design`.
Then create a design-time service for creating the context during migration and invoke `Add-Migration` and `Update-Database` operations in the Package Manager Console.
```csharp
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Leviasan.Sanlog.EntityFrameworkCore;

/// <summary>
/// The design-time service for creating the context during migration.
/// </summary>
/// <remarks>
/// <list type="table">
///     <item>
///        Add-Migration InitialDatabase -Args "appsettings.json DefaultConnection"
///     </item>
///     <item>
///         Update-Database -Args "appsettings.json DefaultConnection"
///     </item>
///     <item>
///         Script-Migration -Args "appsettings.json DefaultConnection"
///     </item>
/// </list>
/// </remarks>
public class SanlogDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SanlogDbContext>
{
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Invalid arguments.</exception>
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
        optionsBuilder.UseSqlServer(connectionString, sqlServerOptions => // Here need Use* method of your database provider
        {
            // Set migration assembly is required
            sqlServerOptions.MigrationsAssembly(typeof(SanlogDbContextDesignTimeDbContextFactory).Assembly.GetName().Name);
        });
        return new SanlogDbContext(optionsBuilder.Options);
    }
}
```

### Logging in a non-trivial app
This first example shows the basics, but it is only suitable for a trivial console app. In the next section, you see how to improve the code by considering scale, performance, configuration, and typical programming patterns.
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Leviasan.Sanlog.EntityFrameworkCore;

internal sealed partial class Program
{
    private static void Main(string[] args)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSanlogEntityFrameworkCore(
            contextConfigure =>
            {
                var connectionString = "Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True";
                contextConfigure.UseSqlServer(connectionString);
            },
            loggingConfigure =>
            {
                loggingConfigure.AppId = Guid.Parse("e6bcc7df-e201-4d0b-02a3-08dbd09ffc89"); // Here need to insert your specific application identifier
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
- Creates an `ILoggerFactory`. The `ILoggerFactory` stores all the configuration that determines where log messages are sent.
- Creates an `ILogger` with a category named "Program". The category is a string that is associated with each message logged by the `ILogger` object. It's used to group log messages from the same class (or category) together when searching or filtering logs.
- Calls `LogInvokedMethod` to log a message at the Information level. The log level indicates the severity of the logged event and is used to filter out less important log messages. The log entry also includes a message template "ClassName: {ClassName}. Method: {MethodName}" and a key-value pairs ClassName = Program and MethodName = Main. The key name (or placeholder) comes from the word inside the curly braces in the template and the value comes from the remaining method argument. Logging compile-time source generation is usually a better alternative to `ILogger` extension methods like `LogInformation`. Logging source generation offers better performance, and stronger typing, and avoids spreading string constants throughout your methods. The tradeoff is that using this technique requires a bit more code.

### Integration with hosts and dependency injection
If your application is using Dependency Injection (DI) or a host such as ASP.NET's WebApplication or Generic Host then you should use `ILoggerFactory` and `ILogger` objects from the DI container rather than creating them directly. Host builders initialize the [default configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host), then add a configured `ILoggerFactory` object to the host's DI container when the host is built. Before the host is built you can adjust the logging configuration via `HostApplicationBuilder.Logging`, `WebApplicationBuilder.Logging`, or similar APIs on other hosts. Hosts also apply logging configuration from default configuration sources such as `appsettings.json` and environment variables. For more information, see [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration).
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Leviasan.Sanlog.EntityFrameworkCore;

public sealed class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        /* ... Another configuration ... */
        builder.AddSanlogEntityFrameworkCore(contextConfigure =>
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            contextConfigure.UseSqlServer(connectionString);
        });
        var app = builder.Build();
        app.Run();
    }
}
```

## Configure logging
Logging configuration is set in code or via external sources, such as config files and environment variables. Using external configuration is beneficial when possible because it can be changed without rebuilding the application. However, some tasks, such as setting logging providers, can only be configured from code.

### Configure logging without code
For apps that use a host, logging configuration is commonly provided by the "Logging" section of `appsettings.{Environment}.json` files. For apps that don't use a host, external configuration sources are set up explicitly or configured in code instead.
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "SanlogLoggerProvider": {
      "AppId": "e6bcc7df-e201-4d0b-02a3-08dbd09ffc89",
      "IncludeScopes": true,
      "UseUtcTimestamp": false,
      "SkipEnabledCheck": false,
      "SuppressThrowing": true,
      "LogLevel": {
        "Microsoft.AspNetCore": "Information"
      }
    }
  }
}
```