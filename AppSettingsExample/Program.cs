using Microsoft.Extensions.Configuration;

namespace AppSettingsExample
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    internal class Program
    {
        static void Main(string[] args)
        {
            var getcur = Directory.GetCurrentDirectory();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration configuration = builder.Build();

            // Access configuration values
            string appName = configuration["ApplicationName"];
            string defaultLogLevel = configuration["Logging:LogLevel:Default"];

            Console.WriteLine($"Application Name: {appName}");
            Console.WriteLine($"Default Log Level: {defaultLogLevel}");

            // Read into an object
            ApiTester settings = new();
            configuration.GetSection(nameof(ApiTester)).Bind(settings);

        }
    }

    public class ApiTester
    {
        public string[] SupportedMethods { get; set; }
        public int MaxRetries { get; set; }
        public string BaseUrl { get; set; }
    }
}