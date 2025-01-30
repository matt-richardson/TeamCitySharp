using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using WireMock.Handlers;
using WireMock.Logging;
using WireMock.Server;
using WireMock.Settings;
using WireMock.Types;

namespace TeamCitySharp.IntegrationTests
{
    public static class Configuration
    {
        private static readonly WireMockServer WiremockServer;
        private static readonly IConfigurationRoot ConfigurationRoot;

        static Configuration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var wiremockFolder = Path.GetFullPath(Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName!, "..", "..", "..", "wiremock"));
            var settings = new WireMockServerSettings
            {
                //the url that clients should use to connect to the wiremock server
                Urls = ["http://localhost:8112"],

                //look at the `wiremock` folder for mappings and files
                WatchStaticMappings = true,
                ReadStaticMappings = true,

                AllowPartialMapping = false,
                QueryParameterMultipleValueSupport = QueryParameterMultipleValueSupport.Ampersand,
                StartAdminInterface = true,
                FileSystemHandler = new LocalFileSystemHandler(wiremockFolder),
                Logger = new WireMockConsoleLogger(),
            };
            
            //uncomment to enable the proxy and record settings
            //EnableProxyAndRecord(settings);
            
            WiremockServer = WireMockServer.Start(settings);
        }

        private static void EnableProxyAndRecord(WireMockServerSettings settings)
        {
            settings.ProxyAndRecordSettings = new ProxyAndRecordSettings
            {
                //the url to your teamcity server
                Url = "http://localhost:8111",

                SaveMapping = true,
                SaveMappingToFile = true,

                ExcludedHeaders = ["Host", "traceparent"],
                PrefixForSavedMappingFile = "proxy_mapping",
            };
        }

        public static string GetAppSetting(string key) => ConfigurationRoot[key];

        public static HttpClient GetWireMockClient() => WiremockServer.CreateClient();
    }
}
