namespace AppSettingsExample
{
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// 
    /// https://github.com/kasajian-ether/AppSettingsExample
    /// 
    /// Loads configuration from appsettings.json file(s), environment variables, and command-line arguments.
    /// Additional settings not typically configured by default:
    /// 1. Environment variable requires an app prefix so the variable names don't conflict with other apps.
    /// 2. Command-line args support switch mappings to allow shorter names.
    /// 3. Support for multiple appsettings.json files in various locations.  See GetAppSettingsFilesToLoad() method for details.  Supports wildcards.
    /// 4. Setting AdditionalAppSettingsFilePath can be specified via environment variable or command-line to point to an additional appsettings.json file.
    ///     This can be multiple paths separated by semi-colon, which of which can be a wildcard.
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
    ///  Example specifying additional appsettings file via environment variable or command-line:
    ///      --AdditionalAppSettingsFilePath %userprofile%\MyOverride\myapp_appsettings_*.json
    ///
    /// </summary>
    internal static class AppSettingsHelper
    {
        private const string AppSettingsJsonFilename = "appsettings.json";
        private const string AppSettingsJsonFilenameWild = "appsettings*.json";
        private const string AdditionalAppSettingsFilePath = "AdditionalAppSettingsFilePath";

        public static IConfiguration GetConfiguration(string appPrefix, IDictionary<string, string> switchMappings)
        {
            // Get the args array, same as what's passed into Main(string[] args) in a console app.
            var commandLineArgs = Environment.GetCommandLineArgs();
            string[] args = commandLineArgs.Length > 1 ? [.. commandLineArgs.Skip(1)] : [];

            // Get the list of appsettings json files and load each one
            ConfigurationBuilder builder = new();
            foreach (var appSettingsFile in GetAppSettingsFilesToLoad(appPrefix, args))
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
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string commonDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            string entryAssemblyDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty) ?? throw new InvalidOperationException();

            // Check for multiple appsettings.json files in the following order.
            //    It will load each and will be merged
            //    Later settings override earlier ones.

            // 1) Check for appsettings.json in exe's (EntryAssembly's) directory.
            {
                var entryAssemblyAppSettings = Path.Combine(entryAssemblyDirectory, AppSettingsJsonFilename);
                if (File.Exists(entryAssemblyAppSettings)) yield return entryAssemblyAppSettings;
            }

            // Prepare wildcard pattern for subsequent searches
            var wild = appPrefix + AppSettingsJsonFilenameWild;

            // 2) Check for MYAPP_appsettings*.json wildcard in exe's (EntryAssembly's) directory.
            {
                var entryAssemblyMyAppSettings = Path.Combine(entryAssemblyDirectory, wild);
                var entryAssemblyMyAppSettingsFiles = GetAllFiles(entryAssemblyMyAppSettings);
                foreach (var item in entryAssemblyMyAppSettingsFiles) yield return item;
            }

            // 3) Check for MYAPP_appsettings*.json wildcard in common documents directory (%public%\Documents).
            {
                var commonDocumentsMyAppSettings = Path.Combine(commonDocumentsPath, wild);
                var commonDocumentsMyAppSettingsFiles = GetAllFiles(commonDocumentsMyAppSettings);
                foreach (var item in commonDocumentsMyAppSettingsFiles) yield return item;
            }

            // 4) Check for MYAPP_appsettings*.json wildcard in user's profile directory (%userprofile%).
            {
                var userProfileMyAppSettings = Path.Combine(userProfilePath, wild);
                var userProfileMyAppSettingsFiles = GetAllFiles(userProfileMyAppSettings);
                foreach (var item in userProfileMyAppSettingsFiles) yield return item;
            }

            // 5) Check for MYAPP_appsettings*.json wildcard in current directory.
            {
                var currentDirectoryMyAppSettings = Path.Combine(Directory.GetCurrentDirectory(), wild);
                var currentDirectoryMyAppSettingsFiles = GetAllFiles(currentDirectoryMyAppSettings);
                foreach (var item in currentDirectoryMyAppSettingsFiles) yield return item;
            }

            // 6) Check for settings pointed to file additionalAppSettingsFilePaths (can be wildcard).
            {
                var additionalAppSettingsFilePaths = HandleAdditionalAppSettingsFilePaths(appPrefix, args);
                foreach (var item in additionalAppSettingsFilePaths) yield return item;
            }
        }

        /// <summary>
        /// If command-line or environment variable has AdditionalAppSettingsFilePath set,
        /// use it locate override appSettings.json files.
        /// Assume it's a filepath that may contain wildcards, get all the files that match.
        /// If the specified filepath doesn't have a directory component, assume the current directory.
        /// </summary>
        private static IEnumerable<string> HandleAdditionalAppSettingsFilePaths(string appPrefix, string[] args)
        {
            // First look for AdditionalAppSettingsFilePath in environment variable and command-line
            //   to see if there's additional appsettings file to load
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: appPrefix)
                .AddCommandLine(args)
                .Build();

            // See if AdditionalAppSettingsFilePath is set.  Bail if not
            var additionalAppSettingsFilePath = configuration[AdditionalAppSettingsFilePath];
            if (string.IsNullOrEmpty(additionalAppSettingsFilePath)) return [];

            return additionalAppSettingsFilePath
                .Split([';'], StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(GetAllFiles);
        }

        /// <summary>
        /// Given a filepath that may contain wildcards, list all the files that match.
        /// If the specified filepath doesn't have a directory component, assume the current directory.
        /// </summary>
        /// <param name="filepathWithWildcard"></param>
        private static string[] GetAllFiles(string filepathWithWildcard)
        {
            var directory = Path.GetDirectoryName(filepathWithWildcard);
            directory ??= Directory.GetCurrentDirectory();

            var filenameWithWildcard = Path.GetFileName(filepathWithWildcard);
            return Directory.GetFiles(directory, filenameWithWildcard, SearchOption.TopDirectoryOnly);
        }
    }
}
