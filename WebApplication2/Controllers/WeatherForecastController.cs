using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Compliance.Classification;

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

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            _logger.LogInformation("CommandType: {CommandType:D}. Parameters: {@Parameters}", CommandType.Text, new Dictionary<string, object?>
            {
                { "Key1", null },
                { "Key2", 15 },
                { "Key4", new byte[3] { 66, 99, 123 } }, // bad story
                { "Key3", new Dictionary<string, object> { { "array", new byte[3] { 66, 99, 123 } } } }
            });
            InvokeStoredProcedure(_logger, LogLevel.Information, null, CommandType.Text, new Dictionary<string, object?>
            {
                { "Key1", null },
                { "Key2", 15 },
                { "Key3", new Dictionary<string, object> { { "array", new byte[3] { 66, 99, 123 } } } }
            });
            s_logger.Invoke(_logger, CommandType.Text, new Dictionary<string, object?>
            {
                { "Key1", null },
                { "Key2", 15 },
                { "Key3", new Dictionary<string, object> { { "array", new byte[3] { 66, 99, 123 } } } }
            }, null);
            var exception = new ArgumentNullException();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
        [HttpPost]
        public IActionResult Post([FromBody] SensitiveRequest request)
        {
            _logger.LogInformation("Request: {@Request}", request);
            return NoContent();
        }

        [LoggerMessage(Message = "CommandType: {CommandType:D}. Parameters: {@Parameters}")]
        public static partial void InvokeStoredProcedure(ILogger logger, LogLevel logLevel, Exception? exception, CommandType? commandType, IDictionary<string, object?> parameters);

        static Action<ILogger, CommandType, IDictionary<string, object?>, Exception?> s_logger
            = LoggerMessage.Define<CommandType, IDictionary<string, object?>>(LogLevel.Information, 1, "CommandType: {CommandType:D}. Parameters: {@Parameters}");

        public sealed record class SensitiveRequest(string Username, [property: UnknownDataClassification] string Password);
    }
}
