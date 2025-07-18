using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sanlog.EntityFrameworkCore.Metadata.Builders
{
    /// <summary>
    /// The configuration of the <see cref="LoggingEntry"/> model.
    /// </summary>
    internal sealed class LoggingEntryConfiguration : IEntityTypeConfiguration<LoggingEntry>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingEntry> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Version).IsRequired(false).IsUnicode(false).HasMaxLength(43);
            _ = builder.Property(x => x.Category).IsRequired(true).IsUnicode(true);
            _ = builder.Property(x => x.EventName).IsRequired(false).IsUnicode(true);
            _ = builder.Property(x => x.Message).IsRequired(false).IsUnicode(true).HasMaxLength(int.MaxValue);
            _ = builder.Property(x => x.Properties).IsRequired(false).IsUnicode(true).HasMaxLength(int.MaxValue);
            _ = builder.HasIndex(x => x.Timestamp).IsDescending();
            _ = builder.HasMany(x => x.Scopes).WithOne().HasForeignKey(x => x.LogEntryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
            _ = builder.HasMany(x => x.Errors).WithOne().HasForeignKey(x => x.LogEntryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
        }
    }
}