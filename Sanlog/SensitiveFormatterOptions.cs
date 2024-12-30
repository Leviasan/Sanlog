using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Sanlog
{
    /// <summary>
    /// Represents the configuration of the <see cref="SensitiveFormatter"/>.
    /// </summary>
    public sealed class SensitiveFormatterOptions
    {
        /// <summary>
        /// The sensitive properties.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Dictionary<SensitiveKeyType, HashSet<string>> _dictionary = [];

        /// <summary>
        /// Gets a value indicating whether the configuration is read-only.
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Registers a property whose value belongs to sensitive data.
        /// </summary>
        /// <param name="type">The sensitive key type.</param>
        /// <param name="property">The property whose value belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is added to the collection; <see langword="false"/> if the element is already present.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="property"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool AddSensitive(SensitiveKeyType type, string property)
        {
            CheckReadOnly(); // InvalidOperationException
            if (!Enum.IsDefined(type))
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveKeyType));
            ArgumentNullException.ThrowIfNull(property);
            return _dictionary.TryGetValue(type, out var hashset) ? hashset.Add(property) : _dictionary.TryAdd(type, [property]);
        }
        /// <summary>
        /// Registers an array of properties whose values belong to sensitive data.
        /// </summary>
        /// <param name="type">The sensitive key type.</param>
        /// <param name="args">An array of properties whose value belongs to sensitive data.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="args"/> or at least one element in the specified array is <see langword="null"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public int AddSensitive(SensitiveKeyType type, params string[] args)
        {
            ArgumentNullException.ThrowIfNull(args);
            if (!Array.TrueForAll(args, x => x is not null))
                throw new ArgumentNullException(nameof(args), "At least one element in the specified array was null.");
            var count = 0;
            foreach (var name in args)
                count += Convert.ToInt32(AddSensitive(type, name)); // InvalidEnumArgumentException + InvalidOperationException
            return count;
        }
        /// <summary>
        /// Resets the configuration to default.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public void Clear()
        {
            CheckReadOnly(); // InvalidOperationException
            _dictionary.Clear();
        }
        /// <summary>
        /// Checks whether the property of the specified key type belongs to sensitive data.
        /// </summary>
        /// <param name="type">The sensitive key type.</param>
        /// <param name="property">The property whose value belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the property of the specified key type belongs to sensitive data; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="property"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        public bool IsSensitive(SensitiveKeyType type, string property)
        {
            if (!Enum.IsDefined(type))
                throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveKeyType));
            ArgumentNullException.ThrowIfNull(property);
            return _dictionary.TryGetValue(type, out var hashset) && hashset.Contains(property);
        }
        /// <summary>
        /// Copies the current to the specified configuration.
        /// </summary>
        /// <param name="configuration">The destination configuration.</param>
        /// <returns>The count of the added element.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="configuration"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="configuration"/> is read-only.</exception>
        public int CopyTo(SensitiveFormatterOptions configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var count = 0;
            foreach (var sensitive in _dictionary)
                count += configuration.AddSensitive(sensitive.Key, [.. sensitive.Value]); // InvalidOperationException
            return count;
        }
        /// <summary>
        /// Makes the configuration read-only.
        /// </summary>
        /// <returns>Returns the current instance.</returns>
        public SensitiveFormatterOptions MakeReadOnly()
        {
            IsReadOnly = true;
            return this;
        }
        /// <summary>
        /// Removes all properties with the specified sensitive key type.
        /// </summary>
        /// <param name="type">The sensitive key type.</param>
        /// <returns><see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>. This method returns <see langword="false"/> if key is not found.</returns>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool RemoveSensitive(SensitiveKeyType type)
        {
            CheckReadOnly(); // InvalidOperationException
            return Enum.IsDefined(type)
                ? _dictionary.Remove(type)
                : throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveKeyType));
        }
        /// <summary>
        /// Removes the specified property whose value belongs to sensitive data.
        /// </summary>
        /// <param name="type">The sensitive key type.</param>
        /// <param name="property">The property whose value belongs to sensitive data.</param>
        /// <returns><see langword="true"/> if the element is successfully found and removed; otherwise, <see langword="false"/>.
        /// This method returns <see langword="false"/> if <paramref name="type"/> or <paramref name="property"/> is not found.</returns>
        /// <exception cref="InvalidEnumArgumentException">The <paramref name="type"/> in invalid.</exception>
        /// <exception cref="InvalidOperationException">The configuration is read-only.</exception>
        public bool RemoveSensitive(SensitiveKeyType type, string property)
        {
            CheckReadOnly(); // InvalidOperationException
            return Enum.IsDefined(type)
                ? _dictionary.TryGetValue(type, out var hashset) && hashset.Remove(property)
                : throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(SensitiveKeyType));
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
    }
    /// <summary>
    /// Represents the sensitive key type.
    /// </summary>
    public enum SensitiveKeyType
    {
        /// <summary>
        /// The segment name of the message template to redact.
        /// </summary>
        SegmentName = 0,
        /// <summary>
        /// The string representation of the dictionary entry key to redact.
        /// </summary>
        DictionaryEntry = 1,
        /// <summary>
        /// Formats <see cref="IEnumerable"/> instance as [*{ElementCount} {Type.Name}*]. Supported if <see cref="Type.IsPrimitive"/>.
        /// </summary>
        CollapsePrimitive = 2
    }
}