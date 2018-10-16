using System;
using Newtonsoft.Json;

namespace ChatService.DataContracts
{
    public class GetMessageDto
    {
        
        [JsonConstructor]
        public GetMessageDto(string text, string senderUsername,DateTime utcNow)
        {
            Text = text;
            SenderUsername = senderUsername;
            UtcTime = utcNow;
        }
        
        public GetMessageDto(Message message)
        {
            Text = message.Text;
            SenderUsername = message.SenderUsername;
            UtcTime = message.UtcTime;
        }

        public string Text { get; }
        public string SenderUsername { get; }
        public DateTime UtcTime { get; }
    }
}