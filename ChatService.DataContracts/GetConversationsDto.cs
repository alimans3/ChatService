using System;
using Newtonsoft.Json;

namespace ChatService.DataContracts
{
    public class GetConversationsDto
    {
        [JsonConstructor]
        public GetConversationsDto(string id, UserProfile recipient, DateTime lastModifiedDateUtc)
        {
            Id = id;
            Recipient = recipient;
            LastModifiedDateUtc = lastModifiedDateUtc;
        }
        public GetConversationsDto(string username, Conversation conversation,UserProfile recipient)
        {
            Id = conversation.Id;
            Recipient = recipient;
            LastModifiedDateUtc = conversation.LastModifiedDateUtc;
        }

        public static string GetRecipient(string username, Conversation conversation)
        {
            var index = conversation.Participants.IndexOf(username);
            var recipientIndex = index == 0 ? 1 : 0;
            return conversation.Participants[recipientIndex];
        }

        public string Id { get; }
        public UserProfile Recipient { get; }
        public DateTime LastModifiedDateUtc { get; }
        
    }
}