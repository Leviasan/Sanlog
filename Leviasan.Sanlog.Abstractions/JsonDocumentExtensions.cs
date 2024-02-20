using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Provides <see cref="JsonDocument"/> extension methods.
    /// </summary>
    internal static class JsonDocumentExtensions
    {
        /// <summary>
        /// Converts the JSON document to the collection of key/value pairs that provide document properties.
        /// </summary>
        /// <param name="document">The JSON document to parse.</param>
        /// <returns>The collection of key/value pairs that provide document properties.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="document"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <paramref name="document"/> is disposed.</exception>
        public static IReadOnlyDictionary<string, string?> ToStringDictionary(this JsonDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            var dictionary = new Dictionary<string, string?>();
            foreach (var property in document.RootElement.EnumerateObject())
                WriteProperty(dictionary, null, property);
            return dictionary;

            static void WriteProperty(Dictionary<string, string?> dictionary, string? prefix, JsonProperty property)
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    WriteProperty(dictionary, $"{prefix}{property.Name}.", property);
                }
                else if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    var index = 0;
                    foreach (var element in property.Value.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var elementProperty in element.EnumerateObject())
                                WriteProperty(dictionary, $"{prefix}{property.Name}[{index}].", elementProperty);
                        }
                        else
                        {
                            WriteString(dictionary, $"{prefix}{property.Name}[{index}]", element);
                        }
                        index++;
                    }
                }
                else
                {
                    WriteString(dictionary, $"{prefix}{property.Name}", property.Value);
                }
            }
            static void WriteString(Dictionary<string, string?> dictionary, string key, JsonElement element) => dictionary.Add(key, element.ToString());
        }
    }
}