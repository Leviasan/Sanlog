using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sanlog
{
    /// <summary>
    /// Represents the configuration of the sensitive data.
    /// </summary>
    public sealed class SensitiveConfiguration
    {
        /// <summary>
        /// The registered sensitive data.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<Type, HashSet<string>> _sensitiveData = [];

        /// <summary>
        /// Gets a value indicating whether the configuration is read-only.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Registers a property whose value belongs to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="object"/></term>
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
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool Add(Type type, string property)
        {
            CheckReadOnly(); // InvalidOperationException
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(property);
            return _sensitiveData.TryGetValue(type, out var hashset) ? hashset.Add(property) : _sensitiveData.TryAdd(type, [property]);
        }
        /// <summary>
        /// Registers an array of properties whose values belong to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="object"/></term>
        ///         <description>The property name of the message template.</description>
        ///     </item>
        ///     <item>
        ///         <term><see cref="DictionaryEntry"/></term>
        ///         <description>The string representation of the dictionary entry key.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        /// <param name="type">The sensetive key type.</param>
        /// <param name="args">An array of properties whose value belongs to sensitive data.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> or at least one element in the specified array is <see langword="null"/></exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public int Add(Type type, params string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!Array.TrueForAll(args, x => x is not null))
                throw new ArgumentNullException(nameof(args), "At least one element in the specified array was null.");
            var count = 0;
            foreach (var name in args)
                count += Convert.ToInt32(Add(type, name)); // InvalidOperationException
            return count;
        }
        /// <summary>
        /// Throws an exception if the configuration is read-only.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        private void CheckReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("The configuration is read-only.");
        }
        /// <summary>
        /// Removes all keys and values from the configuration.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public void Clear()
        {
            CheckReadOnly(); // InvalidOperationException
            _sensitiveData.Clear();
        }
        /// <summary>
        /// Checks whether the property of the specified type belongs to sensitive data.
        /// </summary>
        /// <remarks>
        /// Table of the supported types:
        /// <list type="table">
        ///     <item>
        ///         <term><see cref="object"/></term>
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
        /// <returns><see langword="true"/> if property of the specified type belongs to sensitive data; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> or <paramref name="property"/> is <see langword="null"/>.</exception>
        public bool Contains(Type type, string property)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(property);
            return _sensitiveData.TryGetValue(type, out var hashset) && hashset.Contains(property);
        }
        /// <summary>
        /// Copies an array of properties whose values belong to sensitive data to the specified configuration from the current instance.
        /// </summary>
        /// <param name="configuration">The configuration of the sensitive data.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="configuration"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="configuration"/> is read-only.</exception>
        public int CopyTo(SensitiveConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var count = 0;
            foreach (var sensitive in _sensitiveData)
                count += configuration.Add(sensitive.Key, [.. sensitive.Value]); // InvalidOperationException
            return count;
        }
        /// <summary>
        /// Makes the configuration is read-only.
        /// </summary>
        public void MakeReadOnly() => IsReadOnly = true;
        /// <summary>
        /// Removes all properties with the specified key.
        /// </summary>
        /// <param name="type">The key of the element to remove.</param>
        /// <returns><see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>. This method returns <see langword="false"/> if key is not found.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="type"/> is <see langword="null"/>.</exception>
        public bool Remove(Type type) => _sensitiveData.Remove(type);
        /// <summary>
        /// Removes the specified property whose values belong to sensitive data.
        /// </summary>
        /// <param name="type">The key of the element to remove.</param>
        /// <param name="property">The property whose value is belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>.
        /// This method returns <see langword="false"/> if <paramref name="type"/> or <paramref name="property"/> is not found.</returns>
        public bool Remove(Type type, string property) => _sensitiveData.TryGetValue(type, out var hashset) && hashset.Remove(property);
    }
}