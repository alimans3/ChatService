using System.Collections.Generic;
using System.Threading.Tasks;
using ChatService.DataContracts;

namespace ChatService.Client
{
    public interface INotificationServiceClient
    {
        Task SendNotification(NotificationDto payload);
    }
}