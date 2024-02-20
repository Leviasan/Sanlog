using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Defines conversions from <see cref="IReadOnlyDictionary{TKey, TValue}"/> object in a model to <see cref="string"/> in the storage.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class StringDictionaryValueConverter : ValueConverter<IReadOnlyDictionary<string, string?>, string?>
    {
        /// <summary>
        /// Provides JSON serialization-related metadata about <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly JsonTypeInfo StringDictionaryJsonTypeInfo = JsonSerializerOptions.Default.GetTypeInfo(typeof(IReadOnlyDictionary<string, string?>));

        /// <summary>
        /// Initializes a new instance of the <see cref="StringDictionaryValueConverter"/> class.
        /// </summary>
        public StringDictionaryValueConverter() : base(dictionary => Serialize(dictionary), json => Deserialize(json)) { }

        /// <summary>
        /// Converts objects when writing data to the store.
        /// </summary>
        /// <param name="dictionary">The object to convert.</param>
        /// <returns>The string representation of the dictionary.</returns>
        private static string? Serialize(IReadOnlyDictionary<string, string?> dictionary)
            => JsonSerializer.Serialize(dictionary, StringDictionaryJsonTypeInfo) is string json && json != "{}" ? json : null;
        /// <summary>
        /// Converts objects when reading data from the store.
        /// </summary>
        /// <param name="json">The object to convert.</param>
        /// <returns>The <see cref="Dictionary{TKey, TValue}"/> that represents json string.</returns>
        private static IReadOnlyDictionary<string, string?> Deserialize(string? json)
            => json is not null ? (IReadOnlyDictionary<string, string?>)JsonSerializer.Deserialize(json, StringDictionaryJsonTypeInfo)! : new Dictionary<string, string?>(0);
    }
}
