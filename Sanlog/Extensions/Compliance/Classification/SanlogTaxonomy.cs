using Microsoft.Extensions.Compliance.Classification;

namespace Sanlog.Extensions.Compliance.Classification
{
    /// <summary>
    /// Represents data classification.
    /// </summary>
    internal static class SanlogTaxonomy
    {
        /// <summary>
        /// Gets the name of the taxonomy.
        /// </summary>
        public static string TaxonomyName => typeof(SanlogTaxonomy).FullName!;
        /// <summary>
        /// Gets the value to represent sensitive data.
        /// </summary>
        public static DataClassification Sensitive => new(TaxonomyName, nameof(Sensitive));
    }
}