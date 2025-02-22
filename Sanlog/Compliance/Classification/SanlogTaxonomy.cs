using Microsoft.Extensions.Compliance.Classification;

namespace Sanlog.Compliance.Classification
{
    /// <summary>
    /// Provides data classifications.
    /// </summary>
    public static class SanlogTaxonomy
    {
        /// <summary>
        /// Gets the name of classification taxonomy.
        /// </summary>
        public static string TaxonomyName => typeof(SanlogTaxonomy).FullName!;
        /// <summary>
        /// Gets the value to represent sensitive data.
        /// </summary>
        public static DataClassification Sensitive => new(TaxonomyName, nameof(Sensitive));
    }
}