using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Sanlog;
using Sanlog.EntityFrameworkCore;

namespace WebApplication2
{
    internal sealed class SanlogDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SanlogDbContext>
    {
        public SanlogDbContext CreateDbContext(string[] args)
        {
            if (args.Length != 2)
                throw new InvalidOperationException("Invalid arguments count");

            var configurationBuilder = new ConfigurationBuilder();
            _ = configurationBuilder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
            _ = configurationBuilder.AddJsonFile(args[0]);
            var configuration = configurationBuilder.Build()!;

            var optionsBuilder = new DbContextOptionsBuilder<SanlogDbContext>();
            var connectionString = configuration.GetConnectionString(args[1]);
            _ = optionsBuilder.UseSqlServer(connectionString, serverOptions => // Here need Use* method of your database provider
            {
                // [Required] Set migration assembly
                _ = serverOptions.MigrationsAssembly(typeof(SanlogDesignTimeDbContextFactory).Assembly.GetName().Name);
            });
            return new SanlogDbContext(optionsBuilder.Options, Options.Create(new SanlogLoggerOptions()));
        }
    }
}
