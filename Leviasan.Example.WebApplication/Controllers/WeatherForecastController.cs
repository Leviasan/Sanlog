using System;
using System.Collections.Generic;
using System.Linq;
using Leviasan.Sanlog;
using Leviasan.Sanlog.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Leviasan.Example.WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
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

        [HttpGet("GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();
        }
        [HttpGet("ThrowErrors")]
        public IActionResult ThrowErrors()
        {
            var invalid = new InvalidOperationException();
            var notsupported = new NotSupportedException(null, invalid);
            var program = new InvalidProgramException(null, notsupported);
            throw new AggregateException(program, program);
        }
        [HttpGet("GetApps")]
        public IEnumerable<LoggingApplication> GetApps([FromServices] SanlogDbContext context)
        {
            var apps = context.LogApps.Include(x => x.LogEntries).ToList();
            return apps;
        }
    }
}
