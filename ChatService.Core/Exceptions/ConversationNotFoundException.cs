using System;

namespace ChatService.Core.Exceptions
{
	public class ConversationNotFoundException:Exception
    {
        public ConversationNotFoundException(string message): base (message)
        {
        }
    }
}
