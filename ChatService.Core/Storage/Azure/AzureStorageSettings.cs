namespace ChatService.Core.Storage.Azure
{
    public class AzureStorageSettings
    {

        public string ConnectionString { get; set; }
        public string ProfilesTableName { get; set; }
        public string UserConversationsTable { get; set; }
        public string MessagesTable { get; set; }
        
    }
}