using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Leviasan.Sanlog.EntityFrameworkCore
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
        /// Initializes a new instance of the <see cref="SanlogDbContext"/> class using the specified options.
        /// The <see cref="DbContext.OnConfiguring(DbContextOptionsBuilder)"/> method will still be called to allow further configuration of the options.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public SanlogDbContext(DbContextOptions<SanlogDbContext> options) : base(options) { }

        /// <summary>
        /// The <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="LoggingApplication"/>.
        /// </summary>
        public DbSet<LoggingApplication> LogApps => Set<LoggingApplication>();
        /// <summary>
        /// The <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="LoggingLevel"/>.
        /// </summary>
        public DbSet<LoggingLevel> LogLevels => Set<LoggingLevel>();
        /// <summary>
        /// The <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="LoggingEntry"/>.
        /// </summary>
        public DbSet<LoggingEntry> LogEntries => Set<LoggingEntry>();
        /// <summary>
        /// The <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="LoggingEntryProperty"/>.
        /// </summary>
        public DbSet<LoggingEntryProperty> LogEntryProperties => Set<LoggingEntryProperty>();
        /// <summary>
        /// The <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="LoggingScope"/>.
        /// </summary>
        public DbSet<LoggingScope> LogScopes => Set<LoggingScope>();
        /// <summary>
        /// The <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="LoggingScopeProperty"/>.
        /// </summary>
        public DbSet<LoggingScopeProperty> LogScopeProperties => Set<LoggingScopeProperty>();
        /// <summary>
        /// The <see cref="DbSet{TEntity}"/> that can be used to query and save instances of <see cref="LoggingError"/>.
        /// </summary>
        public DbSet<LoggingError> LogErrors => Set<LoggingError>();

        /// <summary>
        /// Query to get logging entries by the specified scope value.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>The collection of logging entries that contains the specified scope value.</returns>
        public IQueryable<LoggingEntry> FindByScopeValue(string name, string value)
        {
            return LogEntries
                .Where(entry => entry.Scopes!.Any(
                    scope => scope.Properties!.Any(
                        property => property.Type == name && property.Message == value)))
                .OrderByDescending(x => x.DateTime);
        }
        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Debug.Assert(optionsBuilder is not null);
            _ = optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);
            _ = optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
            base.OnConfiguring(optionsBuilder);
        }
        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Debug.Assert(modelBuilder is not null);
            base.OnModelCreating(modelBuilder);
            _ = modelBuilder.ApplyConfiguration(new LoggingApplicationConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingLevelConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingErrorConfiguration());
            _ = modelBuilder.ApplyConfiguration(new LoggingEntryConfiguration());
        }
    }
}