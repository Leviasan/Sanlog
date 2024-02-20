using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Provides the <see cref="ILoggingBuilder"/> extension methods.
    /// </summary>
    public static class ILoggingBuilderExtensions
    {
        /// <summary>
        /// Adds Sanlog logging services with EntityFrameworkCore storage.
        /// </summary>
        /// <param name="builder">The logging builder.</param>
        /// <param name="contextConfigure">The options for context.</param>
        /// <param name="loggingConfigure">The options for logging.</param>
        /// <returns>The logging builder.</returns>
        public static ILoggingBuilder AddSanlogEntityFrameworkCore(this ILoggingBuilder builder, Action<DbContextOptionsBuilder> contextConfigure, Action<SanlogLoggerOptions>? loggingConfigure = default)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            builder.AddConfiguration();
            _ = builder.Services.AddDbContextFactory<SanlogDbContext>(contextConfigure);
            _ = builder.Services.AddSingleton<AsyncSanlogDbContextWriter>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                serviceProvider => new SanlogLoggerProvider(
                    eventWriter: serviceProvider.GetRequiredService<AsyncSanlogDbContextWriter>(),
                    optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services);
            _ = builder.Services.Configure<SanlogLoggerOptions>(options => options.JsonSerializerOptions.Converters.Add(new EntityEntryJsonConverter()));
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }

        /// <summary>
        /// Represents a mechanism to write events to the storage in async mode.
        /// </summary>
        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
        private sealed class AsyncSanlogDbContextWriter : IEventWriter
        {
            /// <summary>
            /// The factory for creating <see cref="SanlogDbContext"/> instances.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly IDbContextFactory<SanlogDbContext> _contextFactory;

            /// <summary>
            /// Initializes a new instance of the <see cref="AsyncSanlogDbContextWriter"/> class with the specified factory for creating <see cref="SanlogDbContext"/> instances.
            /// </summary>
            /// <param name="contextFactory">The factory for creating <see cref="SanlogDbContext"/> instances.</param>
            /// <exception cref="ArgumentNullException">The <paramref name="contextFactory"/> is <see langword="null"/>.</exception>
            public AsyncSanlogDbContextWriter(IDbContextFactory<SanlogDbContext> contextFactory) => _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

            /// <inheritdoc/>
            public async ValueTask WriteAsync(LoggingEntry item, CancellationToken cancellationToken = default)
            {
                using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                _ = await context.LogEntries.AddAsync(item, cancellationToken).ConfigureAwait(false);
                _ = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}