using System.Collections.Generic;
using System.Threading.Tasks;
using ChatService.DataContracts;

namespace ChatService.Client
{
    public interface INotificationService
    {
        Task SendNotification(string username, Payload payload);
    }
}