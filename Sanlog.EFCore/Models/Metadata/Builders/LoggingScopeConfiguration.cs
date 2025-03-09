﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sanlog.Models;

namespace Sanlog.EntityFrameworkCore.Models.Metadata.Builders
{
    /// <summary>
    /// The configuration of the <see cref="LoggingScope"/> model.
    /// </summary>
    internal sealed class LoggingScopeConfiguration : IEntityTypeConfiguration<LoggingScope>
    {
        /// <inheritdoc/>
        public void Configure(EntityTypeBuilder<LoggingScope> builder)
        {
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Type).IsRequired(true).IsUnicode(false);
            _ = builder.Property(x => x.Message).IsRequired(false).IsUnicode(true);
            _ = builder.Property(x => x.Properties).IsRequired(false).IsUnicode(true).HasMaxLength(int.MaxValue);
        }
    }
}