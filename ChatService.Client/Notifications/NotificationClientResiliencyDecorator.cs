using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ChatService.Core.Storage.Polly;
using ChatService.DataContracts;

namespace ChatService.Client.Notifications
{
    public class NotificationClientResiliencyDecorator : INotificationServiceClient
    {
        private readonly INotificationServiceClient notificationClient;
        private readonly IResiliencyPolicy resiliencyPolicy;

        public NotificationClientResiliencyDecorator(INotificationServiceClient notificationClient,
            IResiliencyPolicy resiliencyPolicy)
        {
            this.resiliencyPolicy = resiliencyPolicy;
            this.notificationClient = notificationClient;
        }

        public async Task SendNotification(NotificationDto payload)
        {
            await resiliencyPolicy.ExecuteAsync(() => notificationClient.SendNotification(payload));
        }
    }
}
