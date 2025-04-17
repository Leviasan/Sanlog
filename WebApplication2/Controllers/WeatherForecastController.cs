using System.Data;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public partial class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("CommandType: {CommandType:D}. Parameters: {@Parameters}", null, new Dictionary<string, object?>
            {
                { "Key1", null },
                { "Key2", 15 }
            });

            InvokeStoredProcedure(_logger, LogLevel.Information, null, null, new Dictionary<string, object?>
            {
                { "Key1", null },
                { "Key2", 15 }
            });

            var exception = new ArgumentNullException();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [LoggerMessage(Message = "CommandType: {CommandType:D}. Parameters: {@Parameters}")]
        public static partial void InvokeStoredProcedure(ILogger logger, LogLevel logLevel, Exception? exception, CommandType? commandType, IDictionary<string, object?> parameters);
    }
}
