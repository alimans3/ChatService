using System;

namespace ChatService.DataContracts
{
    public class ConversationAddedNotification : Notification
    {
        public string ConversationId;
        
        public ConversationAddedNotification(string conversationId, DateTime timeStamp) : base("ConversationAdded", timeStamp)
        {
            ConversationId = conversationId;
        }
        
    }
}