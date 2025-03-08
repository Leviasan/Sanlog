using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Sanlog.Storage;

namespace Sanlog.Models.Metadata.ValueConversion
{
    /// <summary>
    /// Defines conversions from <see cref="IReadOnlyList{T}"/> object in a model
    /// where T is <see cref="KeyValuePair{TKey, TValue}"/> where TKey and TValue are <see cref="string"/> to <see cref="string"/> type in the store.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class ListKvp2StringValueConverter : ValueConverter<IReadOnlyList<KeyValuePair<string, string?>>?, string?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListKvp2StringValueConverter"/> class.
        /// </summary>
        public ListKvp2StringValueConverter() : base(
            convertToProviderExpression: static dictionary => Serialize(dictionary),
            convertFromProviderExpression: static json => Deserialize(json))
        { }

        /// <summary>
        /// Converts objects when writing data to the store.
        /// </summary>
        /// <param name="collection">The object to convert.</param>
        /// <returns>The string representation of the list.</returns>
        private static string? Serialize(IReadOnlyList<KeyValuePair<string, string?>>? collection)
        {
            var json = JsonSerializer.Serialize(collection, typeof(IReadOnlyList<KeyValuePair<string, string?>>), SourceGenerationContext.Default);
            return json == "[]" ? null : json;
        }
        /// <summary>
        /// Converts objects when reading data from the store.
        /// </summary>
        /// <param name="json">The object to convert.</param>
        /// <returns>The <see cref="IReadOnlyList{T}"/> that represents json string.</returns>
        private static IReadOnlyList<KeyValuePair<string, string?>>? Deserialize(string? json)
        {
            if (json is not null)
            {
                var obj = JsonSerializer.Deserialize(json, typeof(IReadOnlyList<KeyValuePair<string, string?>>), SourceGenerationContext.Default);
                return obj as IReadOnlyList<KeyValuePair<string, string?>>;
            }
            return null;
        }
    }
}