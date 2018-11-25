using System;
using ChatService.Client;
using ChatService.Core;
using ChatService.Core.Storage;
using ChatService.Core.Storage.Azure;
using ChatService.Core.Storage.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus;
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
            var azureStorageSettings = GetSettings<AzureStorageSettings>(Configuration);
            var azureServiceBusSettings = GetSettings<AzureServiceBusSettings>(Configuration);

            var profileCloudTable = new AzureCloudTable(azureStorageSettings.ConnectionString, azureStorageSettings.ProfilesTableName);
            var profileStore = new AzureTableProfileStore(profileCloudTable);
            var messagesCloudTable = new AzureCloudTable(azureStorageSettings.ConnectionString, azureStorageSettings.MessagesTable);
            var userConversationsCloudTable = new AzureCloudTable(azureStorageSettings.ConnectionString, azureStorageSettings.UserConversationsTable);
            var conversationStore = new AzureConversationStore(messagesCloudTable,userConversationsCloudTable);

            services.AddSingleton<IQueueClient>(new QueueClient(azureServiceBusSettings.ConnectionString,
                azureServiceBusSettings.QueueName));
            services.AddSingleton<INotificationServiceClient,NotificationServiceBusClient>();
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
        
        public static T GetSettings<T>(IConfiguration configuration) where T : new()
        {
            var config = configuration.GetSection(typeof(T).Name);
            T settings = new T();
            config.Bind(settings);
            return settings;
        }
    }
}
