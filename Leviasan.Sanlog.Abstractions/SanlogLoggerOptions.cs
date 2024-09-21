using System;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents <see cref="SanlogLogger"/> configuration.
    /// </summary>
    public sealed class SanlogLoggerOptions : LogDefineOptions
    {
        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        public Guid AppId { get; set; }
        /// <summary>
        /// Indicates whether need to include external scope data. By default <see langword="false"/>.
        /// </summary>
        public bool IncludeScopes { get; set; }
        /// <summary>
        /// Gets or sets the callback function to retrieve the application version. By default the assembly version of the executable process.
        /// </summary>
        public Func<Version?>? OnRetrieveVersion { get; set; } = () => Assembly.GetEntryAssembly()?.GetName().Version;
        /// <summary>
        /// The list of the sensitive data.
        /// </summary>
        public SensitiveConfiguration SensitiveConfiguration { get; } = new();
    }
}