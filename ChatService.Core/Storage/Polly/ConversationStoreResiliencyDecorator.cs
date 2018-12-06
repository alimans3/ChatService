using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using ChatService.DataContracts;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

namespace ChatService.Core.Storage.Polly
{
    public class ConversationStoreResiliencyDecorator : IConversationStore
    {
        private readonly IConversationStore conversationStore;
        private readonly IResiliencyPolicy resiliencyPolicy;

        public ConversationStoreResiliencyDecorator(IConversationStore conversationStore, IResiliencyPolicy resiliencyPolicy)
        {
            this.conversationStore = conversationStore;
            this.resiliencyPolicy = resiliencyPolicy;
        }

        public async Task<Conversation> AddConversation(Conversation conversation)
        {
           return await resiliencyPolicy.ExecuteAsync(() =>  conversationStore.AddConversation(conversation));
        }

        public async Task<Message> AddMessage(string conversationId, Message message)
        {
            return await resiliencyPolicy.ExecuteAsync(() =>  conversationStore.AddMessage(conversationId,message));
        }

        public async Task<ResultMessages> GetConversationMessages(string conversationId, string startCt, string endCt, int limit = 50)
        {
            return await resiliencyPolicy.ExecuteAsync(() =>  conversationStore.GetConversationMessages(conversationId,startCt,endCt,limit));
        }

        public async Task<ResultConversations> GetConversations(string userName, string startCt, string endCt, int limit = 50)
        {
            return await resiliencyPolicy.ExecuteAsync(() => conversationStore.GetConversations(userName,startCt,endCt,limit));
        }
    }
}
