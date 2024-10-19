using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Represents the database context of the logger.
    /// </summary>
    /// <remarks>
    /// By default used overridden logger factory <see cref="NullLoggerFactory.Instance"/>.
    /// By default context use tracking strategy <see cref="QueryTrackingBehavior.NoTrackingWithIdentityResolution"/>.
    /// </remarks>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class SanlogDbContext : DbContext
    {
        /// <summary>
        /// The service retrieving details about the tenant.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ITenantService _tenantService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SanlogDbContext"/> class using the specified options.
        /// The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/> method will still be called to allow further configuration of the options.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        /// <param name="service">The service retrieving details about the tenant.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="service"/> is <see langword="null"/>.</exception>
        [RequiresDynamicCode("EF Core isn't fully compatible with NativeAOT, and running the application may generate unexpected runtime failures.")]
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures." +
            " Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details.")]
        public SanlogDbContext(DbContextOptions<SanlogDbContext> options, ITenantService service) : base(options)
            => _tenantService = service ?? throw new ArgumentNullException(nameof(service));

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
        /// Gets the dbset that can be used to query and save instances of <see cref="TenantClient"/>.
        /// </summary>
        public DbSet<TenantClient> TenantClients => Set<TenantClient>();

        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Debug.Assert(optionsBuilder is not null);
            _ = optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);
            _ = optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
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
            _ = modelBuilder.ApplyConfiguration(new TenantClientConfiguration());
            _ = modelBuilder.Entity<LoggingApplication>().HasQueryFilter(x => x.TenantId == _tenantService.TenantId);
            _ = modelBuilder.Entity<LoggingEntry>().HasQueryFilter(x => x.TenantId == _tenantService.TenantId);
            _ = modelBuilder.Entity<LoggingScope>().HasQueryFilter(x => x.TenantId == _tenantService.TenantId);
            _ = modelBuilder.Entity<LoggingError>().HasQueryFilter(x => x.TenantId == _tenantService.TenantId);
            _ = modelBuilder.Entity<TenantClient>().HasQueryFilter(x => x.Id == _tenantService.TenantId);
            base.OnModelCreating(modelBuilder);
        }
    }
}