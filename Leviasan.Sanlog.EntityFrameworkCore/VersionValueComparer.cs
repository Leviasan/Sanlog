using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Defines the snapshotting and comparison actions for <see cref="Version"/> type.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class VersionValueComparer : ValueComparer<Version>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionValueComparer"/> class.
        /// </summary>
        public VersionValueComparer() : base(static (x, y) => EqualityComparer<Version>.Default.Equals(x, y), static (x) => x.GetHashCode(), static (x) => (Version)x.Clone()) { }
    }
}