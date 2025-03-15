using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sanlog.EntityFrameworkCore.Metadata.Builders
{
    /// <summary>
    /// The configuration of the <see cref="LoggingApplication"/> model.
    /// </summary>
    internal sealed class LoggingApplicationConfiguration : IEntityTypeConfiguration<LoggingApplication>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingApplication> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Application).IsRequired(true).IsUnicode(true).HasMaxLength(450);
            _ = builder.Property(x => x.Environment).IsRequired(true).IsUnicode(true).HasMaxLength(450);
            _ = builder.Property(x => x.TenantId).ValueGeneratedNever();
            _ = builder.HasIndex(x => new { x.Application, x.Environment, x.TenantId }).IsUnique();
            _ = builder.HasMany<LoggingEntry>().WithOne().HasForeignKey(x => x.AppId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
            _ = builder.HasOne<LoggingTenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
        }
    }
}