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
            _ = builder.Property(x => x.Type).IsRequired(true);
            _ = builder.Property(x => x.Message).IsRequired(false);
            _ = builder.Property(x => x.StackTrace).IsRequired(false).HasMaxLength(int.MaxValue);
            _ = builder.Property(x => x.Source).IsRequired(false);
            _ = builder.Property(x => x.HelpLink).IsRequired(false);
            _ = builder.Property(x => x.TargetSite).IsRequired(false);
            _ = builder.HasOne(x => x.LogEntry).WithMany(x => x.Errors).HasForeignKey(x => x.LogEntryId).OnDelete(DeleteBehavior.Cascade).IsRequired(true);
            _ = builder.HasMany(x => x.InnerException).WithOne(x => x.ParentException).HasForeignKey(x => x.ParentExceptionId).OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}