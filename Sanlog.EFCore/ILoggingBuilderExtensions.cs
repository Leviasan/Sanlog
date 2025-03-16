using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Extension methods for registering logger in an <see cref="ILoggingBuilder"/>.
    /// </summary>
    public static class ILoggingBuilderExtensions
    {
        /// <summary>
        /// Adds Sanlog logger to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="loggingConfigure">A callback to configure the <see cref="Sanlog.SanlogLoggerProvider"/>.</param>
        /// <param name="contextConfigure">A callback to configure the <see cref="DbContextOptionsBuilder"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/> to use.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="builder"/> or <paramref name="contextConfigure"/> is <see langword="null"/>.</exception>
        public static ILoggingBuilder AddSanlog(
            this ILoggingBuilder builder,
            Action<DbContextOptionsBuilder> contextConfigure,
            Action<SanlogLoggerOptions>? loggingConfigure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            builder.AddConfiguration();
            builder.Services
                .AddSanlogInfrastructure(builder => builder.SetHandler<SanlogLoggerProvider, LoggingEntryMessageHandler>())
                .AddPooledDbContextFactory<SanlogDbContext>((sp, x) =>
                {
                    var opts = sp.GetRequiredService<IOptions<SanlogLoggerOptions>>().Value;
                    _ = x.UseLoggerFactory(NullLoggerFactory.Instance);
                    _ = x.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
                    _ = x.AddInterceptors(new SanlogDbContext.TenantValidatorInterceptor(opts.AppId, opts.TenantId));
                    contextConfigure.Invoke(x);
                })
                .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>());
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services);
            if (loggingConfigure is not null)
                _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }
    }
}

// ## Logging at trivial app
// This first example shows the basics, but it is only suitable for a trivial console app. In the next section, you see how to improve the code by considering scale, performance, configuration, and typical programming patterns. Example  for //database provider: `Microsoft.EntityFrameworkCore.SqlServer`.
// ```csharp
// internal sealed partial class Program
// {
//     private static void Main(string[] args)
//     {
//         using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSanlogLogging(
//             contextConfigure =>
//             {
//                 // Configure your database provider
//                 var connectionString = "Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True";
//                 contextConfigure.UseSqlServer(connectionString);
//             },
//             loggingConfigure =>
//             {
//                 // [Required] Need to insert your appl and tenant identifiers
//                 loggingConfigure.AppId = Guid.Parse("e6bcc7df-e201-4d0b-02a3-08dbd09ffc89");
//                 loggingConfigure.TenantId = Guid.Parse("45732ee0-72a0-4c8e-8fbb-6b2df4cc3094");
//             }));
//         ILogger logger = factory.CreateLogger(nameof(Program));
//         LogInvokedMethod(logger, null, nameof(Program), nameof(Main));
//         Console.WriteLine("Finished");
//     }
//     [LoggerMessage(Level = LogLevel.Information, Message = "ClassName: {ClassName}. Method: {MethodName}")]
//     static partial void LogInvokedMethod(ILogger logger, Exception? exception, string className, string methodName);
// }
// ```
// The preceding example:
// -Creates an `ILoggerFactory`. The `ILoggerFactory` stores all the configuration and determines where log messages are sent.
// - Creates an `ILogger` with a category named "Program". The category is a string associated with each message logged by the `ILogger` object. It's used to group log messages from the same class (or category) together while searching or // filtering logs.
// - Calls `LogInvokedMethod` to log a message at the Information level. The log level indicates the logged event's severity and filters out less important log messages. The log entry also includes a message template "ClassName: / {ClassName}. /Method: {MethodName}" and a key-value pairs ClassName = Program and MethodName = Main. The key name (or placeholder) comes from the word inside the curly braces in the template and the value comes from the remaining method / argument. /Logging compile-time source generation is usually a better alternative to `ILogger` extension methods like `LogInformation`. Logging source generation offers better performance, and stronger typing, and avoids spreading  string /constants /throughout your methods. The tradeoff is that using this technique requires a bit more code.
// 
// ## Integration with hosts and dependency injection
// If your application uses Dependency Injection (DI) or a host such as ASP.NET's WebApplication or Generic Host then you should use `ILoggerFactory` and `ILogger` objects from the DI container rather than creating them directly. Host / builders/ initialize the [default configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host), then add a configured `ILoggerFactory` object to the host's DI container when the host is built. Before the host is / built you /can adjust the logging configuration via `HostApplicationBuilder.Logging`, `WebApplicationBuilder.Logging`, or similar APIs on other hosts. Hosts also apply logging configuration from default configuration sources such as // `appsettings.json` and environment variables. For more information, see[Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration).
// ```csharp
// using Microsoft.EntityFrameworkCore;
// 
// internal sealed class Program
// {
//     private static void Main(string[] args)
//     {
//         var builder = WebApplication.CreateBuilder(args);
//         /* ... Another configuration ... */
//         builder.Logging.AddSanlogLogging(
//             contextConfigure: contextConfigure =>
//             {
//                 var connectionString = builder.Configuration.GetConnectionString("LoggerConnection");
//                 contextConfigure.UseSqlServer(connectionString);
//             });
//         var app = builder.Build();
//         /* ... Configuring pipeline ... */
//         app.Run();
//     }
// }
// ```