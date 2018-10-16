using Microsoft.WindowsAzure.Storage.Table;

namespace ChatService.Core.Storage.Azure
{
    public class ProfileTableEntity : TableEntity
    {
        public ProfileTableEntity() // default constructor is mandatory
        {
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}