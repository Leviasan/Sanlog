using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// The model configuration of the <see cref="LoggingScopeProperty"/> model.
    /// </summary>
    internal sealed class LoggingScopePropertyConfiguration : IEntityTypeConfiguration<LoggingScopeProperty>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingScopeProperty> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedOnAdd();
            _ = builder.Property(x => x.Key).IsRequired(true);
            _ = builder.Property(x => x.Value).IsRequired(false).HasMaxLength(int.MaxValue);
        }
    }
}