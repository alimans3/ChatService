using System;
using Newtonsoft.Json;

namespace ChatService.DataContracts
{
    public class Payload
    {
        public static string ConversationType = "ConversationAdded";
        public static string MessageType = "MessageAdded";

        [JsonConstructor]
        public Payload(string type, string conversationId, DateTime timeStamp)
        {
            Type = type;
            ConversationId = conversationId;
            TimeStamp = timeStamp;
        }
        
        public string Type { get; set; }
        public string ConversationId { get; set; }
        public DateTime TimeStamp { get; set; }

        public override bool Equals(object obj)
        {
            var payload = obj as Payload;
            return payload.Type == Type && payload.ConversationId == ConversationId && payload.TimeStamp == TimeStamp;
        }
    }
}