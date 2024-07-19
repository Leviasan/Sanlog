using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Leviasan.Sanlog
{
    /// <summary>
    /// Represents <see cref="SanlogLogger"/> configuration.
    /// </summary>
    public sealed class SanlogLoggerOptions
    {
        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        public Guid AppId { get; set; }
        /// <summary>
        /// Indicates whether need to include external scope data. By default <see langword="false"/>.
        /// </summary>
        public bool IncludeScopes { get; set; }
        /// <summary>
        /// Gets or sets the callback function to retrieve the application version. By default the assembly version of the executable process.
        /// </summary>
        public Func<Version?>? OnRetrieveVersion { get; set; } = () => Assembly.GetEntryAssembly()?.GetName().Version;
        /// <summary>
        /// The list of the sensitive data.
        /// </summary>
        public Dictionary<Type, HashSet<string>> SensitiveData { get; } = [];

        /// <summary>
        /// Registers property whose value belongs to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="MessageTemplate"/></term>
        ///         <description>The property name of the message template.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="DictionaryEntry"/></term>
        ///         <description>The string representation of the dictionary entry key.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="type">The sensetive key type.</param>
        /// <param name="property">The property whose value is belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> or <paramref name="property"/> is <see langword="null"/>.</exception>
        public bool RegisterSensitiveData(Type type, string property)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(property);
            return SensitiveData.TryGetValue(type, out var hashset) ? hashset.Add(property) : SensitiveData.TryAdd(type, [property]);
        }
    }
}