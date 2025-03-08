using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Sanlog.Storage.ChangeTracking
{
    /// <summary>
    /// Defines the snapshotting and comparison actions for <see cref="IReadOnlyList{T}"/> type
    /// where T is <see cref="KeyValuePair{TKey, TValue}"/> where TKey and TValue are <see cref="string"/>.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class ListKvp2StringValueComparer : ValueComparer<IReadOnlyList<KeyValuePair<string, string?>>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListKvp2StringValueComparer"/> class.
        /// </summary>
        public ListKvp2StringValueComparer() : base(
            equalsExpression: static (x, y) => x != null && y != null && x.SequenceEqual(y),
            hashCodeExpression: static x => x.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
            snapshotExpression: static x => x.ToList())
        { }
    }
}