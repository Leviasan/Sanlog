using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Defines the snapshotting and comparison actions for <see cref="Version"/> type.
    /// </summary>
    internal sealed class StringVersionValueComparer : ValueComparer<Version?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringVersionValueComparer"/> class.
        /// </summary>
        public StringVersionValueComparer() : base(
            equalsExpression: static (x, y) => EqualityComparer<Version>.Default.Equals(x, y),
            hashCodeExpression: static (x) => x != null ? x.GetHashCode() : 0,
            snapshotExpression: static (x) => x != null ? (Version)x.Clone() : null)
        { }
    }
}