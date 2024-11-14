using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Represents the database context of the logger.
    /// </summary>
    /// <remarks>
    /// By default used overridden logger factory <see cref="NullLoggerFactory.Instance"/>.
    /// By default context use tracking strategy <see cref="QueryTrackingBehavior.NoTrackingWithIdentityResolution"/>.
    /// </remarks>
    public sealed class SanlogDbContext : DbContext
    {
        /// <summary>
        /// The listener to be called whenever a <see cref="SanlogLoggerOptions"/> changes.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly IDisposable? _changeTokenRegistration;
        /// <summary>
        /// The logger options.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SanlogLoggerOptions _loggerOptions;
        /// <summary>
        /// The <see cref="ISaveChangesInterceptor"/> for validating tenant and application identifiers.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly TenantValidatorInterceptor _interceptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogDbContext"/> class using the specified options.
        /// The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/> method will still be called to allow further configuration of the options.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        /// <param name="loggerOptionsMonitor">Used for notifications when <see cref="SanlogLoggerOptions"/> instances change.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> or <paramref name="loggerOptionsMonitor"/> is <see langword="null"/>.</exception>
        [RequiresDynamicCode("EF Core isn't fully compatible with NativeAOT, and running the application may generate unexpected runtime failures.")]
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details.")]
        public SanlogDbContext(DbContextOptions<SanlogDbContext> options, IOptionsMonitor<SanlogLoggerOptions> loggerOptionsMonitor) : base(options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(loggerOptionsMonitor);
            _loggerOptions = loggerOptionsMonitor.CurrentValue;
            _interceptor = new TenantValidatorInterceptor(() => _loggerOptions);
            _changeTokenRegistration = loggerOptionsMonitor.OnChange(OnChangeOptions);
        }

        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingApplication"/>.
        /// </summary>
        internal DbSet<LoggingApplication> LogApps => Set<LoggingApplication>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingLevel"/>.
        /// </summary>
        internal DbSet<LoggingLevel> LogLevels => Set<LoggingLevel>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingEntry"/>.
        /// </summary>
        internal DbSet<LoggingEntry> LogEntries => Set<LoggingEntry>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingScope"/>.
        /// </summary>
        internal DbSet<LoggingScope> LogScopes => Set<LoggingScope>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingError"/>.
        /// </summary>
        internal DbSet<LoggingError> LogErrors => Set<LoggingError>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingTenant"/>.
        /// </summary>
        internal DbSet<LoggingTenant> LogTenants => Set<LoggingTenant>();

        /// <inheritdoc/>
        public override void Dispose()
        {
            _changeTokenRegistration?.Dispose();
            base.Dispose();
        }
        /// <inheritdoc/>
        public override ValueTask DisposeAsync()
        {
            _changeTokenRegistration?.Dispose();
            return base.DisposeAsync();
        }
        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Debug.Assert(optionsBuilder is not null);
            _ = optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);
            _ = optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
            _ = optionsBuilder.AddInterceptors(_interceptor);
            base.OnConfiguring(optionsBuilder);
        }
        /// <inheritdoc/>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            Debug.Assert(configurationBuilder is not null);
            _ = configurationBuilder.Properties<Version>().HaveConversion<StringVersionValueConverter, StringVersionValueComparer>();
            _ = configurationBuilder.Properties<IReadOnlyDictionary<string, string?>>().HaveConversion<StringDictionaryValueConverter, StringDictionaryValueComparer>();
            base.ConfigureConventions(configurationBuilder);
        }
        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Debug.Assert(modelBuilder is not null);
            _ = modelBuilder.ApplyConfiguration(new LoggingApplicationConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingEntryConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingErrorConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingLevelConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingScopeConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingTenantConfiguration());
            _ = modelBuilder.Entity<LoggingApplication>().HasQueryFilter(x => x.TenantId == _loggerOptions.TenantId);
            _ = modelBuilder.Entity<LoggingEntry>().HasQueryFilter(x => x.TenantId == _loggerOptions.TenantId);
            _ = modelBuilder.Entity<LoggingScope>().HasQueryFilter(x => x.TenantId == _loggerOptions.TenantId);
            _ = modelBuilder.Entity<LoggingError>().HasQueryFilter(x => x.TenantId == _loggerOptions.TenantId);
            _ = modelBuilder.Entity<LoggingTenant>().HasQueryFilter(x => x.Id == _loggerOptions.TenantId);
            base.OnModelCreating(modelBuilder);
        }
        /// <summary>
        /// The action to be invoked when <see cref="SanlogLoggerOptions"/> has changed.
        /// </summary>
        /// <param name="options">The changed logger options.</param>
        /// <param name="_">The name of the options.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        private void OnChangeOptions(SanlogLoggerOptions options, string? _) => _loggerOptions = options ?? throw new ArgumentNullException(nameof(options));

        /// <summary>
        /// Represents the <see cref="ISaveChangesInterceptor"/> for validating tenant and application identifiers.
        /// </summary>
        private sealed class TenantValidatorInterceptor : SaveChangesInterceptor
        {
            /// <summary>
            /// The function to get the current logger configuration.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly Func<SanlogLoggerOptions> _configure;

            /// <summary>
            /// Initializes a new instance of the <see cref="TenantValidatorInterceptor"/> class with the specified logger options.
            /// </summary>
            /// <param name="configure">The function to get the current logger configuration.</param>
            /// <exception cref="ArgumentNullException">The <paramref name="configure"/> is <see langword="null"/>.</exception>
            public TenantValidatorInterceptor(Func<SanlogLoggerOptions> configure) => _configure = configure ?? throw new ArgumentNullException(nameof(configure));

            /// <inheritdoc/>
            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                var saving = true;
                if (eventData.Context is SanlogDbContext context)
                {
                    var options = _configure.Invoke();
                    var tenant = context.LogTenants.Find(options.TenantId);
                    var application = context.LogApps.Find(options.AppId);
                    saving = application is not null && tenant is not null && application.TenantId == tenant.Id;
                }
                return saving ? base.SavingChanges(eventData, result) : InterceptionResult<int>.SuppressWithResult(0);
            }
            /// <inheritdoc/>
            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                var saving = true;
                if (eventData.Context is SanlogDbContext context)
                {
                    var options = _configure.Invoke();
                    var tenant = await context.LogTenants.FindAsync([options.TenantId], cancellationToken).ConfigureAwait(true);
                    var application = await context.LogApps.FindAsync([options.AppId], cancellationToken).ConfigureAwait(true);
                    saving = application is not null && tenant is not null && application.TenantId == tenant.Id;
                }
                return saving ? await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(true) : InterceptionResult<int>.SuppressWithResult(0);
            }
        }
    }
}