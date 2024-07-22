using System.Text.Json.Serialization;

namespace Leviasan.Sanlog
{
    [JsonSerializable(typeof(LoggingEntry), GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    internal sealed partial class SourceGenerationContext : JsonSerializerContext { }
}