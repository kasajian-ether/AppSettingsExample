namespace AppSettingsExample
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Loads configuration from appsettings.json file(s), environment variables, and command-line arguments.
    /// Additional settings not typically configured by default:
    /// 1. Environment variable requires an app prefix so the variable names don't conflict with other apps.
    /// 2. Command-line args support switch mappings to allow shorter names.
    /// 3. Support for multiple appsettings.json files in various locations.  See GetAppSettingsFilesToLoad() method for details.
    /// 4. Setting AdditionalAppSettingsFilePath can be specified via environment variable or command-line to point to an additional appsettings.json file.
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
    internal static class AppSettingsHelper
    {
        private const string AppSettingsJsonFilename = "appsettings.json";
        private const string AdditionalAppSettingsFilePath = "AdditionalAppSettingsFilePath";

        public static IConfiguration GetConfiguration(string appPrefix, IDictionary<string, string> switchMappings)
        {
            // Get the args array, same as what's passed into Main(string[] args) in a console app.
            var commandLineArgs = Environment.GetCommandLineArgs();
            string[] args = commandLineArgs.Length > 1 ? [.. commandLineArgs.Skip(1)] : [];

            // Get the list of appsettings json files to be loaded
            var appSettingsFilesToLoad = GetAppSettingsFilesToLoad(appPrefix, args);

            // Load all the appsettings json files found
            ConfigurationBuilder builder = new();
            foreach (var appSettingsFile in appSettingsFilesToLoad)
            {
                builder.AddJsonFile(appSettingsFile, optional: true, reloadOnChange: true);
                Console.WriteLine("AppSettings file processed: {0}", appSettingsFile);
            }

            // Also add environment variables and command-line args
            IConfigurationRoot configuration = builder
                .AddEnvironmentVariables(prefix: appPrefix)
                .AddCommandLine(args, switchMappings)
                .Build();

            return configuration;
        }

        public static IEnumerable<string> GetAppSettingsFilesToLoad(string appPrefix, string[] args)
        {
            // Check for multiple appsettings.json files in the following order.
            //    It will load each and will be merged
            //    Later settings override earlier ones.

            // - Check for appsettings.json in exe's (EntryAssembly's) directory.
            // - Check for MYAPP_appsettings.json in exe's (EntryAssembly's) directory.
            // - Check for MYAPP_appsettings.json in common documents directory (%public%\Documents).
            // - Check for MYAPP_appsettings.json in user's profile directory (%userprofile%).
            // - Check for MYAPP_appsettings.json in current directory.
            // - Check for settings pointed to file additionalAppSettingsFilePath.

            // First look for AdditionalAppSettingsFilePath in environment variable and command-line
            //   to see if there's additional appsettings file to load
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: appPrefix)
                .AddCommandLine(args)
                .Build();

            var additionalAppSettingsFilePath = configuration[AdditionalAppSettingsFilePath];
            if (!string.IsNullOrEmpty(additionalAppSettingsFilePath) || File.Exists(additionalAppSettingsFilePath)) yield return additionalAppSettingsFilePath;

            string entryAssemblyPath = System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty;
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string commonDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);

            // Check appsettings.json EntryAssembly directory
            string entryAssemblyDirectory = Path.GetDirectoryName(entryAssemblyPath) ?? throw new InvalidOperationException();
            var entryAssemblyAppSettings = Path.Combine(entryAssemblyDirectory, AppSettingsJsonFilename);
            if (File.Exists(entryAssemblyAppSettings)) yield return entryAssemblyAppSettings;

            // Check MYAPP_appsettings.json in EntryAssembly directory
            var entryAssemblyMyAppSettings = Path.Combine(entryAssemblyDirectory, appPrefix + AppSettingsJsonFilename);
            if (File.Exists(entryAssemblyMyAppSettings)) yield return entryAssemblyMyAppSettings;

            // Check MYAPP_appsettings.json in common documents directory
            var commonDocumentsMyAppSettings = Path.Combine(commonDocumentsPath, appPrefix + AppSettingsJsonFilename);
            if (File.Exists(commonDocumentsMyAppSettings)) yield return commonDocumentsMyAppSettings;

            // Check MYAPP_appsettings.json in user's profile directory
            var userProfileMyAppSettings = Path.Combine(userProfilePath, appPrefix + AppSettingsJsonFilename);
            if (File.Exists(userProfileMyAppSettings)) yield return userProfileMyAppSettings;

            // Check MYAPP_appsettings.json in current directory
            var currentDirectoryMyAppSettings = Path.Combine(Directory.GetCurrentDirectory(), appPrefix + AppSettingsJsonFilename);
            if (File.Exists(currentDirectoryMyAppSettings)) yield return currentDirectoryMyAppSettings;
        }
    }
}
