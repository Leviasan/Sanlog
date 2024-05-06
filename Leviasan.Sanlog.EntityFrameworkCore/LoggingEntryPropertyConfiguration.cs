using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingEntryProperty"/> model.
    /// </summary>
    internal sealed class LoggingEntryPropertyConfiguration : IEntityTypeConfiguration<LoggingEntryProperty>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingEntryProperty> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedOnAdd();
            _ = builder.Property(x => x.Key).IsRequired(true);
            _ = builder.Property(x => x.Value).IsRequired(false).HasMaxLength(int.MaxValue);
        }
    }
}