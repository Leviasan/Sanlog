using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Defines the snapshotting and comparison actions for <see cref="IReadOnlyDictionary{TKey, TValue}"/> type where TKey is <see cref="string"/> and TValue is <see cref="string"/>.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class StringDictionaryValueComparer : ValueComparer<IReadOnlyDictionary<string, string?>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringDictionaryValueComparer"/> class.
        /// </summary>
        public StringDictionaryValueComparer() : base(
            equalsExpression: static (x, y) => ComparisonExpression(x, y),
            hashCodeExpression: static x => HashCodeGenerator(x),
            snapshotExpression: static x => x.ToDictionary()) { }

        /// <summary>
        /// Indicates whether instances are equal.
        /// </summary>
        /// <param name="first">The first dictionary.</param>
        /// <param name="second">The second dictionary.</param>
        /// <returns><see langword="true"/> if the specified instances is equal; otherwise, <see langword="false"/>.</returns>
        private static bool ComparisonExpression(IReadOnlyDictionary<string, string?>? first, IReadOnlyDictionary<string, string?>? second)
        {
            if (first is not null && second is not null)
            {
                foreach (var kvp in first)
                {
                    if (!second.ContainsKey(kvp.Key)) return false;
                    if (!StringComparer.OrdinalIgnoreCase.Equals(kvp.Value, second[kvp.Key])) return false;
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Generates a hash code of the specified dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary to generate hash.</param>
        /// <returns>A hash code for the specified dictionary.</returns>
        private static int HashCodeGenerator(IReadOnlyDictionary<string, string?> dictionary) => dictionary.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode()));
    }
}