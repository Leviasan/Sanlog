using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sanlog.EFCore
{
    /// <summary>
    /// The model configuration of the <see cref="TenantClient"/> model.
    /// </summary>
    internal sealed class TenantClientConfiguration : IEntityTypeConfiguration<TenantClient>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<TenantClient> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedOnAdd();
            _ = builder.Property(x => x.ClientName).IsRequired(true).IsUnicode(true);
            _ = builder.Property(x => x.ClientDescription).IsRequired(false).IsUnicode(true);
            _ = builder.HasIndex(x => x.ClientName).IsUnique();
        }
    }
}