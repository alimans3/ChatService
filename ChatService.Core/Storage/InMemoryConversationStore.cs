using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.DataContracts;

namespace ChatService.Core.Storage
{
    public class InMemoryConversationStore:IConversationStore
    {
        private readonly ConcurrentDictionary<string,Conversation> store = new ConcurrentDictionary<string,Conversation> ();
        private readonly ConcurrentDictionary<string,List<Message>> messageStore = new ConcurrentDictionary<string, List<Message>>();
        
        public Task<Conversation> AddConversation(Conversation conversation1)
        {
            if (conversation1==null)
            {
                throw new ArgumentNullException(nameof(conversation1));
            }

            if (store.TryAdd(conversation1.Id, conversation1))
            {
                messageStore.TryAdd(conversation1.Id, new List<Message>());
            }
            return Task.FromResult(conversation1);
        }

        public Task<List<Conversation>> GetConversations(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }
            var conversations =new List<Conversation>();
            foreach(var conversation in store.Values)
            {
                if (!conversation.Participants.Contains(userName)) continue;
                conversations.Add(conversation);
            }

            conversations=conversations.OrderBy(conv => conv.LastModifiedDateUtc).ToList();
            return Task.FromResult(conversations);
        }

        public Task<Message> AddMessage(string conversationId, Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                throw new ArgumentNullException(nameof(message.Text));
            }
            if (string.IsNullOrWhiteSpace(message.SenderUsername))
            {
                throw new ArgumentNullException(nameof(message.Text));
            }
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }
            if (!store.TryGetValue(conversationId,out var conversation))
            {
                throw new ConversationNotFoundException("Conversation is not found in storage!");
            }
            if (!messageStore.TryGetValue(conversationId,out var messages))
            {
                throw new ConversationNotFoundException("Conversation is not found in storage!");
            }
            if (!conversation.Participants.Contains(message.SenderUsername))
            {
                throw new InvalidDataException();
            }
            messages.Add(message);
            messageStore[conversationId]= messages;
            conversation.LastModifiedDateUtc = DateTime.UtcNow;
            return Task.FromResult(message);

        }

        public Task<List<Message>> GetConversationMessages(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }
            if (!messageStore.TryGetValue(conversationId, out var messages))
            {
                return Task.FromResult(new List<Message>());
            }

            return Task.FromResult(messages);
        }

        

    }
}
