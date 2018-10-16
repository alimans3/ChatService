using System;

namespace ChatService.Core.Exceptions
{
    public class StorageUnavailableException:Exception
    {
        public StorageUnavailableException(string message) : base(message)
        {
        }
    }
}
