using System;
using System.Reflection;

namespace Sanlog
{
    /// <summary>
    /// Represents <see cref="SanlogLogger"/> configuration.
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
        /// Gets or sets the configuration of the <see cref="SensitiveFormatter"/>.
        /// </summary>
        public SensitiveFormatterOptions? SensitiveConfiguration { get; set; }
        /// <summary>
        /// Gets or sets the configuration of the <see cref="FormattedLogValuesFormatter"/>.
        /// </summary>
        public FormattedLogValuesFormatterOptions? FormattedConfiguration { get; set; }
    }
}