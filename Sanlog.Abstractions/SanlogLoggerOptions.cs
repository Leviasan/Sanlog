using System;
using System.Reflection;

namespace Sanlog
{
    /// <summary>
    /// Represents a <see cref="SanlogLoggerProvider"/> configuration.
    /// </summary>
    public sealed class SanlogLoggerOptions
    {
        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        public Guid AppId { get; set; }
        /// <summary>
        /// Gets or sets the tenant identifier.
        /// </summary>
        public Guid TenantId { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether scopes should be included in the message. By default <see langword="false"/>.
        /// </summary>
        public bool IncludeScopes { get; set; }
        /// <summary>
        /// Gets or sets the callback function to retrieve the application version. By default the assembly version of the executable process.
        /// </summary>
        public Func<Version?>? OnRetrieveVersion { get; set; } = () => Assembly.GetEntryAssembly()?.GetName().Version;
        /// <summary>
        /// Gets or sets the formatted options.
        /// </summary>
        public LoggerFormatterOptions FormattedOptions { get; set; } = new LoggerFormatterOptions(LoggerFormatterOptions.Default);
    }
}