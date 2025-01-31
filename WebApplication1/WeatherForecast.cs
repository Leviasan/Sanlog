using System.Text;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace WebApplication1
{
    public static partial class IServiceCollectionExtensions
    {
        [LoggerMessage(LogLevel.Information, "WeatherForecast created")]
        public static partial void WeatherForecastCreated(this ILogger logger, [LogProperties] WeatherForecast forecast);

        public static void AddCompliance(this IServiceCollection services)
        {
            _ = services.AddRedaction(x =>
            {
                _ = x.SetRedactor<ErasingRedactor>(new DataClassificationSet(SensitiveDataAttribute.DataClassification));
                _ = x.SetHmacRedactor(x =>
                {
                    x.KeyId = 1;
                    x.Key = Convert.ToBase64String(Encoding.ASCII.GetBytes("12312312312312312312312312312312312312321312"));
                }, new DataClassificationSet(PIIDataAttribute.DataClassification));
            });
        }
    }
    public record class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        [SensitiveData]
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        [PIIData]
        public string? Summary { get; set; }
    }
    internal sealed class SensitiveDataAttribute : DataClassificationAttribute
    {
        internal static DataClassification DataClassification { get; } = new DataClassification(nameof(WeatherForecast), nameof(SensitiveDataAttribute));

        public SensitiveDataAttribute() : base(DataClassification) { }
    }
    internal sealed class PIIDataAttribute : DataClassificationAttribute
    {
        internal static DataClassification DataClassification { get; } = new DataClassification(nameof(WeatherForecast), nameof(PIIDataAttribute));

        public PIIDataAttribute() : base(DataClassification) { }
    }
}
