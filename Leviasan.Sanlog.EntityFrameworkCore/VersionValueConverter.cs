using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Leviasan.Sanlog.EntityFrameworkCore
{
    /// <summary>
    /// Defines conversions from <see cref="Version"/> object in a model to <see cref="string"/> in the storage.
    /// </summary>
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "The class is registered in an inversion of control container as part of the dependency injection pattern")]
    internal sealed class VersionValueConverter : ValueConverter<Version, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionValueConverter"/> class.
        /// </summary>
        public VersionValueConverter() : base(static (x) => x.ToString(), static (x) => Version.Parse(x)) { }
    }
}