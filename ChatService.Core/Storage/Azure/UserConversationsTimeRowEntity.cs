using Microsoft.WindowsAzure.Storage.Table;

namespace ChatService.Core.Storage.Azure
{
    public class UserConversationsTimeRowEntity:TableEntity
    {
        public UserConversationsTimeRowEntity()
        {
            
        }
        public string Id { get; set; }
        public string Recipient { get; set; }
    }
}