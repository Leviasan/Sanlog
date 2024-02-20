using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingEntry"/> model.
    /// </summary>
    internal sealed class LoggingApplicationConfiguration : IEntityTypeConfiguration<LoggingApplication>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingApplication> builder)
        {
            _ = builder.Property(x => x.Application).HasMaxLength(450);
            _ = builder.Property(x => x.Environment).HasMaxLength(450);
            _ = builder.HasIndex(x => new { x.Application, x.Environment }).IsUnique();
        }
    }
}
