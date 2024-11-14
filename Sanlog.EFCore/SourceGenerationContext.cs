using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Provides metadata about a set of types that is relevant to JSON serialization.
    /// </summary>
    /// <remarks>Override <see cref="JavaScriptEncoder"/>: <see href="https://github.com/dotnet/runtime/issues/94135"/> espenrl commented on Jul 17.</remarks>
    [JsonSerializable(typeof(IReadOnlyDictionary<string, string?>), GenerationMode = JsonSourceGenerationMode.Metadata)]
    internal sealed partial class SourceGenerationContext : JsonSerializerContext
    {
        static SourceGenerationContext() => Default = new SourceGenerationContext(CreateJsonSerializerOptions(Default));

        private static JsonSerializerOptions CreateJsonSerializerOptions(SourceGenerationContext defaultContext)
        {
            var options = new JsonSerializerOptions(defaultContext.GeneratedSerializerOptions!)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return options;
        }
    }
}