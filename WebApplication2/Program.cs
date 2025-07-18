using Microsoft.AspNetCore.HttpLogging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Compliance.Redaction;
using Sanlog.EntityFrameworkCore;
using Sanlog.Formatters;

namespace WebApplication2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Logging.AddSanlogEntityFrameworkCore(
                contextConfigure: x => x.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True"),
                loggingConfigure: x => x.FormattedOptions.RegisterFormatter<byte[]>(new ByteArrayFormatter(), "R"));
            builder.Services.AddRedaction(c => c.SetFallbackRedactor<ErasingRedactor>());
            builder.Services.AddHttpLogging(c =>
            {
                c.CombineLogs = true;
                c.LoggingFields = HttpLoggingFields.RequestQuery | HttpLoggingFields.RequestPropertiesAndHeaders
                    | HttpLoggingFields.ResponsePropertiesAndHeaders | HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody;
            });

            var app = builder.Build();
            app.UseHttpLogging();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}