using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Leviasan.Sanlog
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
        /// Indicates whether need to include external scope data. By default <see langword="false"/>.
        /// </summary>
        public bool IncludeScopes { get; set; }
        /// <summary>
        /// Gets or sets the culture name.
        /// </summary>
        public string? CultureName { get; set; }
        /// <summary>
        /// Gets or sets the callback function to retrieve the application version. By default the assembly version of the executable process.
        /// </summary>
        public Func<Version?>? OnRetrieveVersion { get; set; } = () => Assembly.GetEntryAssembly()?.GetName().Version;
        /// <summary>
        /// Gets the collection of the named item format properties that belong to sensitive data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal HashSet<string> SensitiveDataType { get; } = [];

        /// <summary>
        /// Registers a key of the <see cref="KeyValuePair{TKey, TValue}"/> whose associated value will be redacted before logging.
        /// </summary>
        /// <param name="item">The key of the <see cref="KeyValuePair{TKey, TValue}"/>.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="item"/> is <see langword="null"/>.</exception>
        public bool RegisterSensitiveData(string item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return item != FormattedLogValuesFormatter.OriginalFormat && SensitiveDataType.Add(item);
        }
    }
}