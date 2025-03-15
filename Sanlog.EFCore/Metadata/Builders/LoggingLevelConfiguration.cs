using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace Sanlog.EntityFrameworkCore.Metadata.Builders
{
    /// <summary>
    /// The configuration of the <see cref="LoggingLevel"/> model.
    /// </summary>
    internal sealed class LoggingLevelConfiguration : IEntityTypeConfiguration<LoggingLevel>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingLevel> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Name).HasConversion<EnumToStringConverter<LogLevel>>().IsRequired(true).IsUnicode(false).HasMaxLength(11);
            _ = builder.HasData(
                new LoggingLevel { Id = (int)LogLevel.Trace, Name = LogLevel.Trace },
                new LoggingLevel { Id = (int)LogLevel.Debug, Name = LogLevel.Debug },
                new LoggingLevel { Id = (int)LogLevel.Information, Name = LogLevel.Information },
                new LoggingLevel { Id = (int)LogLevel.Warning, Name = LogLevel.Warning },
                new LoggingLevel { Id = (int)LogLevel.Error, Name = LogLevel.Error },
                new LoggingLevel { Id = (int)LogLevel.Critical, Name = LogLevel.Critical });
            _ = builder.HasIndex(x => x.Name).IsUnique(true);
            _ = builder.HasMany<LoggingEntry>().WithOne().OnDelete(DeleteBehavior.Restrict).IsRequired(true);
        }
    }
}
