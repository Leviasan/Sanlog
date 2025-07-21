using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Sanlog.EntityFrameworkCore.ChangeTracking
{
    /// <summary>
    /// Defines the snapshotting and comparison actions for <see cref="Dictionary{TKey, TValue}"/> type where TKey and TValue are <see cref="string"/>.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class DictionaryValueComparer : ValueComparer<Dictionary<string, string?>?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryValueComparer"/> class.
        /// </summary>
        public DictionaryValueComparer() : base(
            equalsExpression: static (x, y) => x != null && y != null && x.SequenceEqual(y),
            hashCodeExpression: static x => x != null ? x.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())) : 0,
            snapshotExpression: static x => x != null ? x.ToDictionary() : null)
        { }
    }
}