using System;
using System.Net;
using System.Net.Http;
using ChatService.Client;
using ChatService.Core.Storage.Azure;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChatService.FunctionalTests.Utils
{
    public class TestUtils
    {
        public static IMetricsClient GenerateClient()
        {
            Mock<ILoggerFactory> factory =new Mock<ILoggerFactory>();
            var metricsClientFactory = new MetricsClientFactory(factory.Object,
                TimeSpan.FromSeconds(15));
            return metricsClientFactory.CreateMetricsClient<LoggerMetricsClient>();
        }
        public static void AssertStatusCode(HttpStatusCode statusCode, IActionResult actionResult)
        {
            Assert.IsTrue(actionResult is ObjectResult);
            ObjectResult objectResult = (ObjectResult)actionResult;

            Assert.AreEqual((int)statusCode, objectResult.StatusCode);
        }
        
        public static Uri GetServiceUri()
        {
            string serviceUri =
                Environment.GetEnvironmentVariable("ChatServiceUri");
            if (string.IsNullOrWhiteSpace(serviceUri))
            {
                return null;
            }
 
            return new Uri(serviceUri);
        }
 
        public static ChatServiceClient CreateTestServerAndClient()
        {
            var serviceUri = GetServiceUri();
 
            // we can see this in VSTS to ensure that the deployment verification tests actually ran against the deployment
            Console.WriteLine($"Test Service Uri is {serviceUri}");
 
            if (serviceUri == null)
            {
                var builder = WebHost.CreateDefaultBuilder().UseStartup<Startup>().ConfigureTestServices(s =>
                    s.AddSingleton<INotificationServiceClient>(new Mock<INotificationServiceClient>().Object));
                var server = new TestServer(builder);
                return new ChatServiceClient(server.CreateClient());
            }
 
            var httpClient = new HttpClient {BaseAddress = serviceUri};
            return new ChatServiceClient(httpClient);
        }

    }
}