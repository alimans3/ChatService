using System;

namespace ChatService.Core.Exceptions
{
    public class StorageErrorException : Exception
    {
        public StorageErrorException(string message) : base(message)
        {
        }

        public StorageErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
