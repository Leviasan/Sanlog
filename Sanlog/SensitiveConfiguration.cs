using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Sanlog
{
    /// <summary>
    /// Represents the configuration of the sensitive data.
    /// </summary>
    public sealed class SensitiveConfiguration
    {
        /// <summary>
        /// The dictionary of the sensitive properties.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<SensitiveItemType, HashSet<string>> _dictionary = [];

        /// <summary>
        /// Gets a value indicating whether the configuration is read-only.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Registers a property whose value belongs to sensitive data.
        /// </summary>
        /// <param name="type">The format item type.</param>
        /// <param name="property">The property whose value belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="property"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool Add(SensitiveItemType type, string property)
        {
            CheckReadOnly(); // InvalidOperationException
            if (!Enum.IsDefined(type))
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveItemType));
            ArgumentNullException.ThrowIfNull(property);
            return _dictionary.TryGetValue(type, out var hashset) ? hashset.Add(property) : _dictionary.TryAdd(type, [property]);
        }
        /// <summary>
        /// Registers an array of properties whose values belong to sensitive data.
        /// </summary>
        /// <param name="type">The format item type.</param>
        /// <param name="args">An array of properties whose value belongs to sensitive data.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> or at least one element in the specified array is <see langword="null"/></exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public int Add(SensitiveItemType type, params string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!Array.TrueForAll(args, x => x is not null))
                throw new ArgumentNullException(nameof(args), "At least one element in the specified array was null.");
            var count = 0;
            foreach (var name in args)
                count += Convert.ToInt32(Add(type, name)); // InvalidEnumArgumentException + InvalidOperationException
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
            _dictionary.Clear();
        }
        /// <summary>
        /// Checks whether the property of the specified format item type belongs to sensitive data.
        /// </summary>
        /// <param name="type">The format item type.</param>
        /// <param name="property">The property whose value belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if property of the specified format item type belongs to sensitive data; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="property"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        public bool Contains(SensitiveItemType type, string property)
        {
            if (!Enum.IsDefined(type))
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveItemType));
            ArgumentNullException.ThrowIfNull(property);
            return _dictionary.TryGetValue(type, out var hashset) && hashset.Contains(property);
        }
        /// <summary>
        /// Copies all elements to the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration of the sensitive data.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="configuration"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="configuration"/> is read-only.</exception>
        public int CopyTo(SensitiveConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var count = 0;
            foreach (var sensitive in _dictionary)
                count += configuration.Add(sensitive.Key, [.. sensitive.Value]); // InvalidOperationException
            return count;
        }
        /// <summary>
        /// Makes the configuration read-only.
        /// </summary>
        public void MakeReadOnly() => IsReadOnly = true;
        /// <summary>
        /// Removes all properties with the specified key.
        /// </summary>
        /// <param name="type">The format item type.</param>
        /// <returns><see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>. This method returns <see langword="false"/> if key is not found.</returns>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool Remove(SensitiveItemType type)
        {
            CheckReadOnly(); // InvalidOperationException
            return Enum.IsDefined(type)
                ? _dictionary.Remove(type)
                : throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveItemType));
        }
        /// <summary>
        /// Removes the specified property whose value belongs to sensitive data.
        /// </summary>
        /// <param name="type">The format item type.</param>
        /// <param name="property">The property whose value belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>.
        /// This method returns <see langword="false"/> if <paramref name="type"/> or <paramref name="property"/> is not found.</returns>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool Remove(SensitiveItemType type, string property)
        {
            CheckReadOnly(); // InvalidOperationException
            return Enum.IsDefined(type)
                ? _dictionary.TryGetValue(type, out var hashset) && hashset.Remove(property)
                : throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveItemType));
        }
    }
    /// <summary>
    /// Represents the format item type.
    /// </summary>
    public enum SensitiveItemType
    {
        /// <summary>
        /// The segment name of the message template.
        /// </summary>
        SegmentName = 0,
        /// <summary>
        /// The string representation of the dictionary entry key.
        /// </summary>
        DictionaryEntry = 1
    }
}