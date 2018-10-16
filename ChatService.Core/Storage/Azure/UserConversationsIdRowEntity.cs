using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace ChatService.Core.Storage.Azure
{
    public class UserConversationsIdRowEntity:TableEntity
    {
        public UserConversationsIdRowEntity()
        {
            
        }
        public string Recipient { get; set; }
        public DateTime LastModifiedUtcTime { get; set; }
    }
}