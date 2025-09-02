using Microsoft.Extensions.Configuration;

namespace AppSettingsExample
{
    internal class Program
    {
        private const string AppPrefix = "MYAPP_";

        static void Main()
        {
            var switchMappings = new Dictionary<string, string>
            {
                { "--app", "ApplicationName" }
            };

            var configuration = AppSettingsHelper.GetConfiguration(AppPrefix, switchMappings) ?? throw new InvalidOperationException();

            // Access configuration values individually
            string appName = configuration["ApplicationName"] ?? string.Empty;
            Console.WriteLine($"Application Name: {appName}");

            string defaultLogLevel = configuration["Logging:LogLevel:Default"] ?? string.Empty;
            Console.WriteLine($"Default Log Level: {defaultLogLevel}");

            string extraEntry = configuration["ExtraEntry"] ?? string.Empty;
            Console.WriteLine($"Extra Entry: {extraEntry}");


            // Access configuration values by reading into an object
            ApiTester settings = new();
            configuration.GetSection(nameof(ApiTester)).Bind(settings);

            Console.WriteLine($"Supported Methods: {string.Join(", ", settings.SupportedMethods)}");
            Console.WriteLine($"Max Retries: {settings.MaxRetries}");
            Console.WriteLine($"Base URL: {settings.BaseUrl}");
        }

        private static void OutputEnvironment()
        {
            Console.WriteLine("RuntimeInformation.FrameworkDescription: {0}", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            Console.WriteLine("RuntimeInformation.ProcessArchitecture: {0}", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
            Console.WriteLine("RuntimeInformation.RuntimeIdentifier: {0}", System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier);
            Console.WriteLine("RuntimeInformation.OSDescription: {0}", System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            Console.WriteLine("Environment.OSVersion: {0}", Environment.OSVersion);
            Console.WriteLine("Environment.ProcessorCount: {0}", Environment.ProcessorCount);
            Console.WriteLine("Environment.UserName: {0}", Environment.UserName);
            Console.WriteLine("Environment.UserDomainName: {0}", Environment.UserDomainName);
        }
    }

    public class ApiTester
    {
        public string[] SupportedMethods { get; set; } = Array.Empty<string>();
        public int MaxRetries { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
    }
}
