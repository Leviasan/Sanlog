using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sanlog.EntityFrameworkCore.Metadata.Builders
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingTenant"/> model.
    /// </summary>
    internal sealed class LoggingTenantConfiguration : IEntityTypeConfiguration<LoggingTenant>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingTenant> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedOnAdd();
            _ = builder.Property(x => x.ClientName).IsRequired(true).HasMaxLength(450).IsUnicode(true);
            _ = builder.Property(x => x.ClientDescription).IsRequired(false).IsUnicode(true);
        }
    }
}