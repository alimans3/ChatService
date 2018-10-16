using Microsoft.WindowsAzure.Storage.Table;

namespace ChatService.Core.Storage.Azure
{
    public class MessagesTableEntity:TableEntity
    {
        public MessagesTableEntity()
        {
            
        }

        public string Text { get; set; }
        public string SenderUsername { get; set; }
    }
}