using Microsoft.Extensions.Compliance.Classification;

namespace Sanlog.Extensions.Compliance.Classification
{
    /// <summary>
    /// Indicates data that is classified as sensitive.
    /// </summary>
    public sealed class SensitiveDataClassificationAttribute : DataClassificationAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SensitiveDataClassificationAttribute"/> class.
        /// </summary>
        public SensitiveDataClassificationAttribute() : base(SanlogTaxonomy.Sensitive) { }
    }
}