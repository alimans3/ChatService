using System;

namespace ChatService.DataContracts
{
    public class Message
    {
        public Message(AddMessageDto addMessageDto)
        {
            Text = addMessageDto.Text;
            SenderUsername = addMessageDto.SenderUsername;
            UtcTime = DateTime.UtcNow;
        }
        
        public Message(string text,string senderUsername)
        {
            Text = text;
            SenderUsername = senderUsername;
            UtcTime = DateTime.UtcNow;
        }

        public Message(string text, string senderUsername,DateTime utcTime)
        {
            Text = text;
            SenderUsername = senderUsername;
            UtcTime = utcTime;
        }

        public string Text { get; }
        public string SenderUsername { get; }
        public DateTime UtcTime { get; }

        public override bool Equals(object obj)
        {
            var message = obj as Message;
            return message.Text == Text && message.SenderUsername == SenderUsername && message.UtcTime == UtcTime;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
