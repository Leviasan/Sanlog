using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sanlog.EFCore.Server
{
    public sealed class TenantDbContext : DbContext
    {
        [RequiresDynamicCode("EF Core isn't fully compatible with NativeAOT, and running the application may generate unexpected runtime failures.")]
        [RequiresUnreferencedCode("EF Core isn't fully compatible with trimming, and running the application may generate unexpected runtime failures. Some specific coding pattern are usually required to make trimming work properly, see https://aka.ms/efcore-docs-trimming for more details.")]
        public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

        public DbSet<TenantClient> Clients => Set<TenantClient>();
       

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Debug.Assert(optionsBuilder is not null);
            _ = optionsBuilder.UseLoggerFactory(NullLoggerFactory.Instance);
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Debug.Assert(modelBuilder is not null);
            _ = modelBuilder.ApplyConfiguration(new TenantClientConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }
}