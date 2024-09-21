using Leviasan.Sanlog.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

internal sealed class SanlogDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SanlogDbContext>
{
    public SanlogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SanlogDbContext>();
        // Here need Use* method of your database provider
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True";
        optionsBuilder.UseSqlServer(connectionString, serverOptions =>
        {
            // [Required] Set migration assembly
            serverOptions.MigrationsAssembly(typeof(SanlogDbContextDesignTimeDbContextFactory).Assembly.GetName().Name);
        });
        var context = new SanlogDbContext(optionsBuilder.Options);
        _ = context.Database.HasPendingModelChanges(); // TODO: Replace to unittest
        return context;
    }
}