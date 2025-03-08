using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Sanlog.ChangeTracking
{
    /// <summary>
    /// Defines the snapshotting and comparison actions for <see cref="Version"/> type.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class VersionValueComparer : ValueComparer<Version?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionValueComparer"/> class.
        /// </summary>
        public VersionValueComparer() : base(
            equalsExpression: static (x, y) => EqualityComparer<Version>.Default.Equals(x, y),
            hashCodeExpression: static x => x != null ? x.GetHashCode() : 0,
            snapshotExpression: static x => x != null ? (Version)x.Clone() : null)
        { }
    }
}