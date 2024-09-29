using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Sanlog.EFCore
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
            _ = builder.Property(x => x.Version).HasConversion<VersionValueConverter, VersionValueComparer>().IsRequired(false).IsUnicode(false).HasMaxLength(43);
            _ = builder.Property(x => x.LogLevelId).HasConversion<EnumToNumberConverter<LogLevel, int>>();
            _ = builder.Property(x => x.Category).IsRequired(true).IsUnicode(true);
            _ = builder.Property(x => x.EventName).IsRequired(false).IsUnicode(true);
            _ = builder.Property(x => x.Message).IsRequired(false).IsUnicode(true);
            _ = builder.Property(x => x.Properties).HasConversion<StringDictionaryValueConverter, StringDictionaryValueComparer>().IsRequired(false).IsUnicode(true).HasMaxLength(int.MaxValue);
            _ = builder.HasIndex(x => x.DateTime);
            _ = builder.HasMany(x => x.Scopes).WithOne("LogEntry").HasForeignKey(x => x.LogEntryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
            _ = builder.HasMany(x => x.Errors).WithOne("LogEntry").HasForeignKey(x => x.LogEntryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
            _ = builder.HasOne<TenantClient>("Tenant").WithMany("LogEntries").HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
        }
    }
}