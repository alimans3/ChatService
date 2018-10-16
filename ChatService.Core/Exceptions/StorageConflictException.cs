using System;

namespace ChatService.Core.Exceptions
{
    public class StorageConflictException : Exception
    {
        public StorageConflictException(string message) : base(message)
        {
        }
    }
}
