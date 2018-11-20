using System.Collections.Generic;

namespace ChatService.DataContracts
{
    public class NotificationDto
    {
        public NotificationDto(List<string> usernames, string notification)
        {
            Usernames = usernames;
            Notification = notification;
        }
        
        public List<string> Usernames { get; set; }
        public string Notification { get; set; }
    }
}