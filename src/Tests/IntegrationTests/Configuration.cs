using System.IO;
using Microsoft.Extensions.Configuration;

namespace TeamCitySharp.IntegrationTests
{
    public static class Configuration
    {
        private static readonly IConfigurationRoot configuration;

        static Configuration()
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();        
        }
        
        public static string GetAppSetting(string key) => configuration[key];
    }
}
