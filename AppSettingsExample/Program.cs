namespace AppSettingsExample
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// 
    /// NuGet packages to include:
    /// 
    ///  <ItemGroup>
    ///    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.8" />
    ///    <PackageReference Include="Microsoft.Extensions.Configuration.CommandLine" Version="9.0.8" />
    ///    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="9.0.8" />
    ///    <PackageReference Include="Microsoft.Extensions.Configuration.FileExtensions" Version="9.0.8" />
    ///    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.8" />
    ///    <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="9.0.8" />
    ///  </ItemGroup> 
    /// 
    /// 
    /// Example appsettings.json:
    ///   {
    ///     "ApplicationName": "MyConsoleApp",
    ///     "Logging": {
    ///       "LogLevel": {
    ///         "Default": "Information"
    ///       }
    ///     },
    ///     "ApiTester": {
    ///       "SupportedMethods": [ "GET", "POST", "PUT", "DELETE" ],
    ///       "MaxRetries": 3,
    ///       "BaseUrl": "https://api.example.com"
    ///     }
    ///   }
    /// 
    /// Make sure to set the properties of appsettings.json to "Copy if newer" or "Copy if newer"
    /// 
    /// Example .csproj setting:
    ///  <ItemGroup>
    ///    <None Update="appsettings.json">
    ///      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    ///    </None>
    ///  </ItemGroup>
    /// 
    ///  
    /// Example overriding via environment variable:
    ///    set MYAPP_ApplicationName=NewAppName
    ///    set MYAPP_Logging__LogLevel__Default=Debug
    ///    set MYAPP_ApiTester__SupportedMethods__4=Patch
    ///     
    /// Examples overriding via command-line:
    ///    --ApplicationName=NewAppName
    ///    --Logging:LogLevel:Default=Debug
    ///    --ApiTester:SupportedMethods:4=Patch
    ///    
    ///    --ApplicationName NewAppName
    ///    /ApplicationName=NewAppName
    ///    /ApplicationName NewAppName
    ///    --App NewAppName
    ///
    ///
    /// </summary>
    internal class Program
    {
        private const string AppPrefix = "MYAPP_";

        static void Main(string[] args)
        {
            // Optional alternative mapping for command line arguments
            var switchMappings = new Dictionary<string, string>
            {
                { "--app", "ApplicationName" }
            };

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: AppPrefix)
                .AddCommandLine(args, switchMappings)
                .Build();

            // Access configuration values individually
            string appName = configuration["ApplicationName"];
            string defaultLogLevel = configuration["Logging:LogLevel:Default"];

            Console.WriteLine($"Application Name: {appName}");
            Console.WriteLine($"Default Log Level: {defaultLogLevel}");

            // Access configuration values by reading into an object
            ApiTester settings = new();
            configuration.GetSection(nameof(ApiTester)).Bind(settings);

            Console.WriteLine($"Supported Methods: {string.Join(", ", settings.SupportedMethods)}");
            Console.WriteLine($"Max Retries: {settings.MaxRetries}");
            Console.WriteLine($"Base URL: {settings.BaseUrl}");
        }
    }

    public class ApiTester
    {
        public string[] SupportedMethods { get; set; }
        public int MaxRetries { get; set; }
        public string BaseUrl { get; set; }
    }
}
