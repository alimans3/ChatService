using System;

namespace ChatService.Core.Exceptions
{
    public class ProfileNotFoundException : Exception
    {
        public ProfileNotFoundException(string message) : base(message)
        {
        }
    }
}
