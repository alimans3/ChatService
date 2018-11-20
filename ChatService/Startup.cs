using System;
using ChatService.Client;
using ChatService.Core.Storage;
using ChatService.Core.Storage.Azure;
using ChatService.Core.Storage.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Metrics;

namespace ChatService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            var notificationServiceUri = Configuration.GetSection("Microservices")["NotificationServiceUri"];
            var notificationService = new NotificationServiceClient(new Uri(notificationServiceUri));
            var azureStorageSettings = GetStorageSettings();

            var profileCloudTable = new AzureCloudTable(azureStorageSettings.ConnectionString, azureStorageSettings.ProfilesTableName);
            var profileStore = new AzureTableProfileStore(profileCloudTable);
            var messagesCloudTable = new AzureCloudTable(azureStorageSettings.ConnectionString, azureStorageSettings.MessagesTable);
            var userConversationsCloudTable = new AzureCloudTable(azureStorageSettings.ConnectionString, azureStorageSettings.UserConversationsTable);
            var conversationStore = new AzureConversationStore(messagesCloudTable,userConversationsCloudTable);

            services.AddSingleton<INotificationServiceClient>(notificationService);
            services.AddSingleton<IMetricsClient>(context =>
            {
                var metricsClientFactory = new MetricsClientFactory(context.GetRequiredService<ILoggerFactory>(),
                    TimeSpan.FromSeconds(15));
                return metricsClientFactory.CreateMetricsClient<LoggerMetricsClient>();
            });
            services.AddSingleton<IProfileStore>(context =>
                new ProfileStoreMetricDecorator(profileStore, context.GetRequiredService<IMetricsClient>()));
            services.AddSingleton<IConversationStore>(context =>
                new ConversationStoreMetricDecorator(conversationStore, context.GetRequiredService<IMetricsClient>()));
            services.AddLogging();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                loggerFactory.AddConsole();
                loggerFactory.AddDebug();
            }

            app.UseMvc();
        }


        private AzureStorageSettings GetStorageSettings()
        {
            IConfiguration storageConfiguration = Configuration.GetSection("AzureStorageSettings");
            AzureStorageSettings storageSettings = new AzureStorageSettings();
            storageConfiguration.Bind(storageSettings);
            return storageSettings;
        }
    }
}
