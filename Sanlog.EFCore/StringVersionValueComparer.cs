using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Sanlog.EFCore
{
    /// <summary>
    /// Defines the snapshotting and comparison actions for <see cref="Version"/> type.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
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