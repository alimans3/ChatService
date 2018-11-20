using System;

namespace ChatService.DataContracts
{
    public class MessageAddedNotification : Notification
    {
        public string ConversationId;
        
        public MessageAddedNotification(string conversationId, DateTime timeStamp) : base("MessageAdded", timeStamp)
        {
            ConversationId = conversationId;
        }
    }
}