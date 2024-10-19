using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Defines conversions from <see cref="IReadOnlyDictionary{TKey, TValue}"/> object in a model where TKey is <see cref="string"/> and TValue is <see cref="string"/> to <see cref="string"/> in the store.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class StringDictionaryValueConverter : ValueConverter<IReadOnlyDictionary<string, string?>?, string?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringDictionaryValueConverter"/> class.
        /// </summary>
        public StringDictionaryValueConverter() : base(
            convertToProviderExpression: static dictionary => Serialize(dictionary),
            convertFromProviderExpression: static json => Deserialize(json)) { }

        /// <summary>
        /// Converts objects when writing data to the store.
        /// </summary>
        /// <param name="dictionary">The object to convert.</param>
        /// <returns>The string representation of the dictionary.</returns>
        private static string? Serialize(IReadOnlyDictionary<string, string?>? dictionary)
        {
            return JsonSerializer.Serialize(dictionary, typeof(IReadOnlyDictionary<string, string?>), SourceGenerationContext.Default) is string json && json != "{}" ? json : null;
        }
        /// <summary>
        /// Converts objects when reading data from the store.
        /// </summary>
        /// <param name="json">The object to convert.</param>
        /// <returns>The <see cref="Dictionary{TKey, TValue}"/> that represents json string.</returns>
        private static IReadOnlyDictionary<string, string?>? Deserialize(string? json)
        {
            return json is not null ? (IReadOnlyDictionary<string, string?>)JsonSerializer.Deserialize(json, typeof(IReadOnlyDictionary<string, string?>), SourceGenerationContext.Default)! : null;
        }
    }
}