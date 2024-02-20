using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        /// Indicates whether need to include external scope data.
        /// </summary>
        public bool IncludeScopes { get; set; }
        /// <summary>
        /// The collection of the property names which ignored while writing into <see cref="LoggingEntry.Properties"/> and <see cref="LoggingScope.Properties"/>.
        /// </summary>
        public ICollection<string>? IgnorePropertyKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
        /// <summary>
        /// Gets or sets the callback function to retrieve the application version.
        /// </summary>
        public Func<Version?>? OnRetrieveVersion { get; set; } = () => Assembly.GetEntryAssembly()?.GetName().Version;
        /// <summary>
        /// Gets JSON serializer options.
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
        {
            TypeInfoResolver = new SanlogJsonTypeInfoResolver(),
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        /// <summary>
        /// Suppresses throwing an exception during work logger.
        /// </summary>
        public bool SuppressThrowing { get; set; }
    }
}