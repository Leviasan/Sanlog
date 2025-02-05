using Microsoft.Extensions.Options;

namespace Sanlog
{
    [OptionsValidator]
    internal sealed partial class FormattedLogValuesFormatterOptionsValidator : IValidateOptions<FormattedLogValuesFormatterOptions>
    {
    }
}