using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sanlog.EntityFrameworkCore.ValueConversion
{
    /// <summary>
    /// Provides metadata about a set of types that is relevant to JSON serialization.
    /// </summary>
    /// <remarks>Override <see cref="JavaScriptEncoder"/>: <see href="https://github.com/dotnet/runtime/issues/94135"/> espenrl commented on Jul 17.</remarks>
    [JsonSerializable(typeof(Dictionary<string, string?>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal sealed partial class SourceGenerationContext : JsonSerializerContext
    {
        /// <summary>
        /// Initializes the default context.
        /// </summary>
        static SourceGenerationContext() => Default = new SourceGenerationContext(CreateJsonSerializerOptions(Default));

        /// <summary>
        /// Creates an overriden <see cref="JsonSerializerOptions"/> for the specified context.
        /// </summary>
        /// <param name="context">The context to override settings.</param>
        /// <returns>An overriden <see cref="JsonSerializerOptions"/> for the specified context.</returns>
        private static JsonSerializerOptions CreateJsonSerializerOptions(SourceGenerationContext context)
        {
            JsonSerializerOptions options = new(context.GeneratedSerializerOptions!)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
            return options;
        }
    }
}