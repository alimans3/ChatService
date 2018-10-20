using Microsoft.Extensions.Configuration;

namespace ChatService.Tests
{
    public class UnitTestsUtils
    {
        public static string GetConnectionStringFromConfig()
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables().Build();
            return configBuilder["AzureStorageSettings:connectionString"];
        }
    }
}