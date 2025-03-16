using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sanlog.EntityFrameworkCore;

namespace ConsoleApp2
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddSanlog(
                contextConfigure =>
                {
                    // Configure your database provider
                    var connectionString = "Server=(localdb)\\mssqllocaldb;Database=sanlogdb;Trusted_Connection=True";
                    contextConfigure.UseSqlServer(connectionString);
                },
                loggingConfigure =>
                {
                    // [Required] Need to insert your appl and tenant identifiers
                    loggingConfigure.AppId = Guid.Parse("e6bcc7df-e201-4d0b-02a3-08dbd09ffc89");
                    loggingConfigure.TenantId = Guid.Parse("45732ee0-72a0-4c8e-8fbb-6b2df4cc3094");
                }));
            ILogger logger = factory.CreateLogger(nameof(Program));
            LogInvokedMethod(logger, null, nameof(Program), nameof(Main));
            Console.WriteLine("Finished");
        }
        [LoggerMessage(Level = LogLevel.Information, Message = "ClassName: {ClassName}. Method: {MethodName}")]
        static partial void LogInvokedMethod(ILogger logger, Exception? exception, string className, string methodName);
    }
}
