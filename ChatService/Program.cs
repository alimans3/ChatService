using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IWebHost webHost = CreateWebHostBuilder(args).Build();
            IConfiguration config = webHost.Services.GetRequiredService<IConfiguration>();
            IConfiguration eventFlowConfig = config.GetSection("EventFlowConfig");
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(eventFlowConfig))
            {
                ILoggerFactory loggerFactory = webHost.Services.GetRequiredService<ILoggerFactory>();
                loggerFactory.AddEventFlow(pipeline);
                webHost.Run();
            }
            
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
        }
    }
}
