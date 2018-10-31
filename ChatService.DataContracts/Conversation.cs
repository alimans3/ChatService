using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatService.DataContracts
{
    public class Conversation 
    {
        public Conversation(string id,List<string> participants,DateTime lastModifiedDateUtc)
        {
            Id = id;
            Participants = participants;
            LastModifiedDateUtc = lastModifiedDateUtc;
        }
        public Conversation(AddConversationDto conversationDto)
        {
            Id = GenerateId(conversationDto.Participants);
            Participants = new List<string>(conversationDto.Participants);
            LastModifiedDateUtc = DateTime.UtcNow;
        }
        
        public Conversation(List<string> participants)
        {
            Id = GenerateId(participants);
            Participants = participants;
            LastModifiedDateUtc = DateTime.UtcNow;
        }

        public string Id { get; set; }
        public List<string> Participants { get; set; }
        public DateTime LastModifiedDateUtc { get; set; }
        
        public static string GenerateId(IEnumerable<string> participants)
        {
            return string.Join("$*@", participants.OrderBy(key => key));
        }

        public static List<string> ParseId(string Id)
        {
            return Id.Split("$*@").ToList();
        }
    }
}
