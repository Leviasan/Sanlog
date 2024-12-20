﻿using System;
using System.Reflection;

namespace Sanlog
{
    /// <summary>
    /// Represents <see cref="SanlogLogger"/> configuration.
    /// </summary>
    public sealed class SanlogLoggerOptions
    {
        /// <summary>
        /// Gets the application identifier.
        /// </summary>
        public Guid AppId { get; set; }
        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public Guid TenantId { get; set; }
        /// <summary>
        /// Indicates whether need to include external scope data. By default <see langword="false"/>.
        /// </summary>
        public bool IncludeScopes { get; set; }
        /// <summary>
        /// Gets the callback function to retrieve the application version. By default the assembly version of the executable process.
        /// </summary>
        public Func<Version?>? OnRetrieveVersion { get; set; } = () => Assembly.GetEntryAssembly()?.GetName().Version;
        /// <summary>
        /// The list of the sensitive data.
        /// </summary>
        public SensitiveConfiguration SensitiveConfiguration { get; } = new();
    }
}