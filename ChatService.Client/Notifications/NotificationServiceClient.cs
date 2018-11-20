using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChatService.DataContracts;
using Newtonsoft.Json;

namespace ChatService.Client
{
    public class NotificationServiceClient : INotificationServiceClient
    {
        private HttpClient client;

        public NotificationServiceClient(HttpClient client)
        {
            this.client = client;
        }
        
        public NotificationServiceClient(Uri baseUri)
        {
            client = new HttpClient
            {
                BaseAddress = baseUri
            };
        }
        
        public async Task SendNotification(NotificationDto payload)
        {
            HttpResponseMessage message = await client.PostAsync("api/notification/",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
            if (!message.IsSuccessStatusCode)
            {
                throw new ChatServiceException("Failed to send notifiction",message.ReasonPhrase,message.StatusCode);
            }
        }
    }
}