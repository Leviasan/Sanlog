using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sanlog.EFCore
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingScope"/> model.
    /// </summary>
    internal sealed class LoggingScopeConfiguration : IEntityTypeConfiguration<LoggingScope>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingScope> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Type).IsRequired(true).IsUnicode(false);
            _ = builder.Property(x => x.Message).IsRequired(false).IsUnicode(true);
            _ = builder.Property(x => x.Properties).HasConversion<StringDictionaryValueConverter, StringDictionaryValueComparer>().IsRequired(false).IsUnicode(true).HasMaxLength(int.MaxValue);
            _ = builder.HasOne<TenantClient>("Tenant").WithMany("LogEntries").HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
        }
    }
}