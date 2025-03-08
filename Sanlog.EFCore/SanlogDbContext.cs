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
using Sanlog.Models;
using Sanlog.Models.Metadata.Builders;
using Sanlog.Models.Metadata.ChangeTracking;
using Sanlog.Models.Metadata.ValueConversion;

namespace Sanlog
{
    /// <summary>
    /// Represents a database context of the logger.
    /// </summary>
    /// <remarks>
    /// By default used overridden logger factory <see cref="NullLoggerFactory.Instance"/>.
    /// By default context use tracking strategy <see cref="QueryTrackingBehavior.NoTrackingWithIdentityResolution"/>.
    /// </remarks>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class SanlogDbContext : DbContext
    {
        /// <summary>
        /// The logger configuration.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SanlogLoggerOptions _loggerOptions;
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
        /// <param name="loggerOptions">The configuration of the <see cref="SanlogLoggerProvider"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> or <paramref name="loggerOptions"/> is <see langword="null"/>.</exception>
        [RequiresDynamicCode("EF Core isn't fully compatible with NativeAOT, and running the application may generate unexpected runtime failures.")]
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details.")]
        public SanlogDbContext(DbContextOptions<SanlogDbContext> options, IOptions<SanlogLoggerOptions> loggerOptions) : base(options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(loggerOptions);
            _loggerOptions = loggerOptions.Value;
            _interceptor = new TenantValidatorInterceptor(loggerOptions.Value);
        }

        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingApplication"/>.
        /// </summary>
        public DbSet<LoggingApplication> LogApps => Set<LoggingApplication>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingLevel"/>.
        /// </summary>
        public DbSet<LoggingLevel> LogLevels => Set<LoggingLevel>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingEntry"/>.
        /// </summary>
        public DbSet<LoggingEntry> LogEntries => Set<LoggingEntry>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingScope"/>.
        /// </summary>
        public DbSet<LoggingScope> LogScopes => Set<LoggingScope>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingError"/>.
        /// </summary>
        public DbSet<LoggingError> LogErrors => Set<LoggingError>();
        /// <summary>
        /// Gets the dbset that can be used to query and save instances of <see cref="LoggingTenant"/>.
        /// </summary>
        public DbSet<LoggingTenant> LogTenants => Set<LoggingTenant>();

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
            _ = configurationBuilder
                .Properties<Version>()
                .HaveConversion<VersionValueConverter, VersionValueComparer>();
            _ = configurationBuilder
                .Properties<IReadOnlyList<KeyValuePair<string, string?>>>()
                .HaveConversion<ListKvp2StringValueConverter, ListKvp2StringValueComparer>();
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
        /// Represents the <see cref="ISaveChangesInterceptor"/> for validating tenant and application identifiers.
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of the <see cref="TenantValidatorInterceptor"/> class with the specified logger configuration.
        /// </remarks>
        /// <param name="options">The logger configuration.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="options"/> is <see langword="null"/>.</exception>
        private sealed class TenantValidatorInterceptor(SanlogLoggerOptions options) : SaveChangesInterceptor
        {
            /// <summary>
            /// The logger configuration.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly SanlogLoggerOptions _options = options;

            /// <inheritdoc/>
            public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            {
                var saving = true;
                if (eventData.Context is SanlogDbContext context)
                {
                    var tenant = context.LogTenants.Find(_options.TenantId);
                    var application = context.LogApps.Find(_options.AppId);
                    saving = application is not null && tenant is not null && application.TenantId == tenant.Id;
                }
                return saving ? base.SavingChanges(eventData, result) : InterceptionResult<int>.SuppressWithResult(0);
            }
            /// <inheritdoc/>
            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                var saving = true;
                if (eventData.Context is SanlogDbContext context)
                {
                    var tenant = await context.LogTenants.FindAsync([_options.TenantId], cancellationToken).ConfigureAwait(true);
                    var application = await context.LogApps.FindAsync([_options.AppId], cancellationToken).ConfigureAwait(true);
                    saving = application is not null && tenant is not null && application.TenantId == tenant.Id;
                }
                return saving ? await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(true) : InterceptionResult<int>.SuppressWithResult(0);
            }
        }
    }
}