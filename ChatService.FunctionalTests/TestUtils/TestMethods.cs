using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatService.FunctionalTests.TestUtils
{
    public class TestMethods
    {
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
                var server = new TestServer(WebHost.CreateDefaultBuilder().UseStartup<Startup>());
                return new ChatServiceClient(server.CreateClient());
            }
 
            var httpClient = new HttpClient {BaseAddress = serviceUri};
            return new ChatServiceClient(httpClient);
        }
    }
}