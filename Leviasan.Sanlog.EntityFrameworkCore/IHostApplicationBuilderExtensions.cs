using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Provides the <see cref="IHostApplicationBuilder"/> extension methods.
    /// </summary>
    public static class IHostApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures Sanlog logging services with EntityFrameworkCore storage as a hosted service.
        /// </summary>
        /// <param name="builder">The host application builder.</param>
        /// <param name="contextConfigure">The options for context.</param>
        /// <param name="loggingConfigure">The options for logging.</param>
        /// <returns>The host application builder.</returns>
        public static IHostApplicationBuilder AddSanlogEntityFrameworkCore(this IHostApplicationBuilder builder, Action<DbContextOptionsBuilder> contextConfigure, Action<SanlogLoggerOptions>? loggingConfigure = default)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(contextConfigure);

            // Add configuration
            builder.Logging.AddConfiguration();
            // Register clipboard between loggers and storage
            _ = builder.Services.AddKeyedSingleton(nameof(HostedSanlogDbContextWriter), (serviceProvider, serviceKey) => new ThreadingQueueChannel(new UnboundedChannelOptions()));
            // Register logger provider
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SanlogLoggerProvider>(
                serviceProvider => new SanlogLoggerProvider(
                    eventWriter: serviceProvider.GetRequiredKeyedService<ThreadingQueueChannel>(nameof(HostedSanlogDbContextWriter)),
                    optionsMonitor: serviceProvider.GetRequiredService<IOptionsMonitor<SanlogLoggerOptions>>())));
            LoggerProviderOptions.RegisterProviderOptions<SanlogLoggerOptions, SanlogLoggerProvider>(builder.Services);
            // Register SanlogDbContext factory
            _ = builder.Services.AddDbContextFactory<SanlogDbContext>(contextConfigure);
            // Register background hosted service
            _ = builder.Services.AddHostedService<HostedSanlogDbContextWriter>();
            // Configure logging options
            _ = builder.Services.Configure<SanlogLoggerOptions>(options => options.JsonSerializerOptions.Converters.Add(new EntityEntryJsonConverter()));
            if (loggingConfigure is not null) _ = builder.Services.Configure(loggingConfigure);
            return builder;
        }

        /// <summary>
        /// Represents the background service to write log entries to the database.
        /// </summary>
        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
        private sealed class HostedSanlogDbContextWriter : BackgroundService
        {
            /// <summary>
            /// The threading queue channel.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly ThreadingQueueChannel _channel;
            /// <summary>
            /// The context factory.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly IDbContextFactory<SanlogDbContext> _contextFactory;

            /// <summary>
            /// Initializes a new instance of the <see cref="HostedSanlogDbContextWriter"/> class with the specified threading queue channel and context factory.
            /// </summary>
            /// <param name="channel">The threading queue channel.</param>
            /// <param name="contextFactory">The context factory.</param>
            /// <exception cref="ArgumentNullException">One of the parameters is <see langword="null"/>.</exception>
            public HostedSanlogDbContextWriter([FromKeyedServices(nameof(HostedSanlogDbContextWriter))] ThreadingQueueChannel channel, IDbContextFactory<SanlogDbContext> contextFactory)
            {
                _channel = channel ?? throw new ArgumentNullException(nameof(channel));
                _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            }

            /// <inheritdoc/>
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var loggingEntry = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                    using var context = await _contextFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
                    _ = await context.LogEntries.AddAsync(loggingEntry, stoppingToken).ConfigureAwait(false);
                    _ = await context.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                }
            }
        }
    }
}