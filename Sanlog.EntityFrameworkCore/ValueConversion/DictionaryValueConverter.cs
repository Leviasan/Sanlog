using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sanlog.EntityFrameworkCore.ValueConversion
{
    /// <summary>
    /// Defines conversions from <see cref="Dictionary{TKey, TValue}"/> object in a model
    /// where TKey and TValue are <see cref="string"/> to <see cref="string"/> type in the store.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class DictionaryValueConverter : ValueConverter<Dictionary<string, string>?, string?>
    {
        /// <summary>
        /// Represents an empty json body.
        /// </summary>
        private const string EmptyObject = "{}";

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryValueConverter"/> class.
        /// </summary>
        public DictionaryValueConverter() : base(
            convertToProviderExpression: static dictionary => Serialize(dictionary),
            convertFromProviderExpression: static json => Deserialize(json))
        { }

        /// <summary>
        /// Converts objects when writing data to the store.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>The string representation of the list.</returns>
        private static string? Serialize(Dictionary<string, string>? value)
        {
            string json = value is not null
                ? JsonSerializer.Serialize(value, SourceGenerationContext.Default.DictionaryStringString)
                : EmptyObject;
            return json == EmptyObject ? null : json;
        }
        /// <summary>
        /// Converts objects when reading data from the store.
        /// </summary>
        /// <param name="json">The object to convert.</param>
        /// <returns>The <see cref="IReadOnlyList{T}"/> that represents json string.</returns>
        private static Dictionary<string, string>? Deserialize(string? json)
        {
            if (json is not null and not EmptyObject)
            {
                Dictionary<string, string>? result = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.DictionaryStringString);
                return result;
            }
            return null;
        }
    }
}