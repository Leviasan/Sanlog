# Sanlog.EntityFrameworkCore

Provides a logger that supports saving log entries from different applications in database scheme, separated by an app and tenant identifiers.

## Install the package

From the command-line:

```console
dotnet add package Sanlog.EntityFrameworkCore
```

Or directly in the C# project file:

```xml
<ItemGroup>
  <PackageReference Include="Sanlog.EntityFrameworkCore" Version="[CURRENTVERSION]" />
</ItemGroup>
```

## Get started

### Step 1: Configure logging

Logging configuration is set in code or via external sources, such as config files and environment variables. Using external configuration is beneficial when possible because it can be changed without rebuilding the application.
However, some tasks, such as setting logging providers, can only be configured from code. For apps that use a host, logging configuration is commonly provided by the "Logging" section of `appsettings.{Environment}.json` files.
For apps that don't use a host, external configuration sources are set up explicitly or configured in code instead.
```json
{
  "ConnectionStrings": {
    "LoggerConnection": "Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "SanlogLoggerProvider": {
      "AppId": "e6bcc7df-e201-4d0b-02a3-08dbd09ffc89",
      "TenantId": "45732ee0-72a0-4c8e-8fbb-6b2df4cc3094",
      "IncludeScopes": true,
      "LogLevel": {
        "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Information"
      }
    }
  }
}
```

### Step 2: Migration

- Install NuGet package `Microsoft.EntityFrameworkCore.Tools`.
- Add design-time service `SanlogDesignTimeDbContextFactory` class to your project such as:
```csharp
using System;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

internal sealed class SanlogDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SanlogDbContext>
{
    public SanlogDbContext CreateDbContext(string[] args)
    {
        if (args.Length != 2)
            throw new InvalidOperationException("Invalid arguments count");

        ConfigurationBuilder configurationBuilder = new();
        _ = configurationBuilder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
        _ = configurationBuilder.AddJsonFile(args[0]);
        IConfigurationRoot configuration = configurationBuilder.Build()!;

        DbContextOptionsBuilder<SanlogDbContext> optionsBuilder = new();
        string? connectionString = configuration.GetConnectionString(args[1]);
        _ = optionsBuilder.UseSqlServer(connectionString, serverOptions => // Here need Use* method of your database provider
        {
            // [Required] Set migration assembly
            _ = serverOptions.MigrationsAssembly(typeof(SanlogDesignTimeDbContextFactory).Assembly.GetName().Name);
        });
        return new SanlogDbContext(optionsBuilder.Options, Options.Create(new SanlogLoggerOptions()));
    }
}
```
- Set as Startup Project
- Create a migration `Add-Migration InitialDatabase -Args "appsettings.json LoggerConnection" -Context SanlogDbContext`.
- Apply a migration `Update-Database -Args "appsettings.json LoggerConnection" -Context SanlogDbContext`.

### Step 3: Register your application
Connect to your database provider and invoke the next `insert` commands. Example for MSSQL:
- `insert into dbo.LogTenants values ('45732ee0-72a0-4c8e-8fbb-6b2df4cc3094', 'MyCompanyName', 'MyCompanyDescription')`
- `insert into dbo.LogApps values ('e6bcc7df-e201-4d0b-02a3-08dbd09ffc89', 'MyProjectName', 'MyProjectEnvironment', '45732ee0-72a0-4c8e-8fbb-6b2df4cc3094')`

### How to use logger without use [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder)
```csharp

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sanlog.EntityFrameworkCore;

internal class Program
{
    static async Task Main(string[] args)
    {
        IServiceCollection? services = null;
        using ILoggerFactory factory = LoggerFactory.Create(builder =>
        {
            _ = builder.AddSanlogEntityFrameworkCore(
                contextConfigure: x => x.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True"),
                loggingConfigure: x =>
                {
                    x.AppId = Guid.Parse("e6bcc7df-e201-4d0b-02a3-08dbd09ffc89");
                    x.TenantId = Guid.Parse("45732ee0-72a0-4c8e-8fbb-6b2df4cc3094");
                });
            services = builder.Services;
        });
        ILogger logger = factory.CreateLogger<Program>();
        using ServiceProvider serviceProvider = services!.BuildServiceProvider();

        // Start hosted services manually
        foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        logger.LogInformation("Hello World! Logging is {Description}.", "fun");

        // Stop hosted services manually
        await Task.Delay(20000); // Delay before stop services for correctly writing logs
        foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StopAsync(CancellationToken.None);
        }
    }
}
```

## Attention!
- `ODP.NET` does support `Guid`s. `Guid`s can be inserted into a `RAW(16)` column which is big enough to hold any `Guid` value. But caution needs to be taken in order to handle `Guid`s appropriately.
This is due to the fact that as the .NET `Guid` structure flips the byte values in reverse order for the integer-based parts of the `Guid` values when `Guid(byte[])` constructor is used and when the `ToByteArray()` method on the `Guid` struct is invoked.
More information: https://docs.oracle.com/en/database/oracle/oracle-database/23/odpnt/featGUID.html.
- If using `EFCore.NamingConventions` you must configure `DbContextOptionsBuilder` using invoke `Use*CaseNamingConvention` in both places with the same method.
It is the design time service that implements `IDesignTimeDbContextFactory<SanlogDbContext>` and the second place is while configuring the Sanlog logger.

## Miscellaneous
- Global Query Filters (https://learn.microsoft.com/en-us/ef/core/querying/filters)