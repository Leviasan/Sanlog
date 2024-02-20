using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingError"/> model.
    /// </summary>
    internal sealed class LoggingErrorConfiguration : IEntityTypeConfiguration<LoggingError>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingError> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Data).HasConversion<StringDictionaryValueConverter, StringDictionaryValueComparer>();
            _ = builder.Property(x => x.Properties).HasConversion<StringDictionaryValueConverter, StringDictionaryValueComparer>();
            _ = builder.HasOne(x => x.LogEntry).WithMany(x => x.Errors).HasForeignKey(x => x.LogEntryId).OnDelete(DeleteBehavior.Cascade).IsRequired();
            _ = builder.HasMany(x => x.InnerException).WithOne(x => x.ParentException).HasForeignKey(x => x.ParentExceptionId).OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}