using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChatService.DataContracts;
using Newtonsoft.Json;

namespace ChatService.Client
{
    public class NotificationService: INotificationService
    {
        private HttpClient client;

        public NotificationService(HttpClient client)
        {
            this.client = client;
        }
        
        public NotificationService(Uri baseUri)
        {
            client = new HttpClient
            {
                BaseAddress = baseUri
            };
        }
        
        public async Task SendNotification(string username, Payload payload)
        {
            HttpResponseMessage message = await client.PostAsync($"api/notification/{username}",
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
            if (!message.IsSuccessStatusCode)
            {
                throw new ChatServiceException("Failed to send notifiction",message.ReasonPhrase,message.StatusCode);
            }
        }
    }
}