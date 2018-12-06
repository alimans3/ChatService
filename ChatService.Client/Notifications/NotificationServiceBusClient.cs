using System;
using System.Text;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.DataContracts;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace ChatService.Client
{
    public class NotificationServiceBusClient: INotificationServiceClient
    {
        private IQueueClient client;

        public NotificationServiceBusClient(IQueueClient client)
        {
            this.client = client;
        }
        
        public async Task SendNotification(NotificationDto payload)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            var message = new Microsoft.Azure.ServiceBus.Message(bytes);
           
            await client.SendAsync(message);
            
        }
    }
}