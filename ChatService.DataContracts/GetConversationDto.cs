using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChatService.DataContracts
{
    public class GetConversationDto
    {
        [JsonConstructor]
        public GetConversationDto(string id, List<string> participants, DateTime lastModifiedDateUtc)
        {
            Id = id;
            Participants = participants;
            LastModifiedDateUtc = lastModifiedDateUtc;
        }
        
        public GetConversationDto(Conversation conversation)
        {
            Id = conversation.Id;
            Participants = conversation.Participants;
            LastModifiedDateUtc = conversation.LastModifiedDateUtc;
        }
        public string Id { get; }
        public List<string> Participants { get; }
        public DateTime LastModifiedDateUtc { get; }
        
        
    }
}