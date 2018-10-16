namespace ChatService.DataContracts
{
    public class AddMessageDto
    {
        public AddMessageDto(string text, string senderUsername)
        {
            Text = text;
            SenderUsername = senderUsername;
        }
        public string Text { get; }
        public string SenderUsername { get; }
    }
}
