using System.Text.Json.Serialization;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides metadata about a set of types that is relevant to JSON serialization.
    /// </summary>
    [JsonSerializable(typeof(LoggingEntry), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    internal sealed partial class SourceGenerationContext : JsonSerializerContext { }
}