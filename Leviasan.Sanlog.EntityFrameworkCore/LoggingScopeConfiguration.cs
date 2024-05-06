using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingScope"/> model.
    /// </summary>
    internal sealed class LoggingScopeConfiguration : IEntityTypeConfiguration<LoggingScope>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingScope> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedOnAdd();
            _ = builder.Property(x => x.Type).IsRequired(true);
            _ = builder.Property(x => x.Message).IsRequired(false).HasMaxLength(int.MaxValue);
        }
    }
}