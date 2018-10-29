using Microsoft.Extensions.Logging;

namespace ChatService.Core
{
    public static class Events
    {
        public static readonly EventId ProfileCreated = CreateEvent(nameof(ProfileCreated));
        public static readonly EventId InternalError = CreateEvent(nameof(InternalError));
        public static readonly EventId ProfileNotFound = CreateEvent(nameof(ProfileNotFound));
        public static readonly EventId ProfileAlreadyExists = CreateEvent(nameof(ProfileAlreadyExists));
        public static readonly EventId StorageError = CreateEvent(nameof(StorageError));
        public static readonly EventId ConversationCreated = CreateEvent(nameof(ConversationCreated));
        public static readonly EventId ConversationNotFound = CreateEvent(nameof(ConversationNotFound));
        public static readonly EventId MessageAdded = CreateEvent(nameof(MessageAdded));
        public static readonly EventId UsernameNotFound = CreateEvent(nameof(UsernameNotFound));
        public static readonly EventId ConversationsRequested = CreateEvent(nameof(ConversationsRequested));
        public static readonly EventId MessagesRequested = CreateEvent(nameof(MessagesRequested));
        

        private static EventId CreateEvent(string eventName)
        {
            return new EventId(0, eventName);
        }
    }
}
