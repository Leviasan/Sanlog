using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingEntry"/> model.
    /// </summary>
    internal sealed class LoggingEntryConfiguration : IEntityTypeConfiguration<LoggingEntry>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingEntry> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Version).HasConversion<VersionValueConverter, VersionValueComparer>().HasMaxLength(43);
            _ = builder.Property(x => x.Category).IsRequired(true);
            _ = builder.Property(x => x.EventName).IsRequired(false);
            _ = builder.Property(x => x.Message).IsRequired(false).HasMaxLength(int.MaxValue);
            _ = builder.HasIndex(x => x.DateTime);
        }
    }
}