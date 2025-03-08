using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Sanlog.Storage.ValueConversion
{
    /// <summary>
    /// Defines conversions from <see cref="Version"/> object in a model to <see cref="string"/> type in the store.
    /// </summary>
    [SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection")]
    internal sealed class VersionValueConverter : ValueConverter<Version?, string?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionValueConverter"/> class.
        /// </summary>
        public VersionValueConverter() : base(
            convertToProviderExpression: static x => x != null ? x.ToString() : null,
            convertFromProviderExpression: static x => x != null ? TryParse(x) : null)
        { }

        /// <summary>
        /// Tries to convert a string representation of version number to an equivalent <see cref="Version"/> object.
        /// </summary>
        /// <param name="x">A string that contains version number to convert.</param>
        /// <returns>A <see cref="Version"/> object equivalent of the version number, if the conversion succeeded; otherwise returns <see langword="null"/>.</returns>
        private static Version? TryParse(string x) => Version.TryParse(x, out var result) ? result : null;
    }
}