using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Sanlog.EntityFrameworkCore.Metadata.Builders
{
    /// <summary>
    /// The configuration of the <see cref="LoggingError"/> model.
    /// </summary>
    internal sealed class LoggingErrorConfiguration : IEntityTypeConfiguration<LoggingError>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingError> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Type).IsRequired(true).IsUnicode(false);
            _ = builder.Property(x => x.Message).IsRequired(false).IsUnicode(true);
            _ = builder.Property(x => x.Data).IsRequired(false).IsUnicode(true).HasMaxLength(int.MaxValue);
            _ = builder.Property(x => x.StackTrace).IsRequired(false).IsUnicode(false).HasMaxLength(int.MaxValue);
            _ = builder.Property(x => x.Source).IsRequired(false).IsUnicode(false);
            _ = builder.Property(x => x.HelpLink).IsRequired(false).IsUnicode(true);
            _ = builder.Property(x => x.TargetSite).IsRequired(false).IsUnicode(false);
            _ = builder.HasOne<LoggingEntry>().WithMany(x => x.Errors).HasForeignKey(x => x.LogEntryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
            _ = builder.HasMany(x => x.InnerException).WithOne().HasForeignKey(x => x.ParentExceptionId).OnDelete(DeleteBehavior.ClientCascade).IsRequired(false);
        }
    }
}