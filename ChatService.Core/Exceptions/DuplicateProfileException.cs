using System;

namespace ChatService.Core.Exceptions
{
    public class DuplicateProfileException : Exception
    {
        public DuplicateProfileException(string message) : base(message)
        {
        }
    }
}
