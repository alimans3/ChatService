using System.Collections.Generic;
using System.Threading.Tasks;
using ChatService.DataContracts;
using Microsoft.Extensions.Logging.Metrics;

namespace ChatService.Core.Storage.Metrics
{
    public class ConversationStoreMetricDecorator : IConversationStore
    {
        private readonly IConversationStore store;
        private readonly AggregateMetric AddConversationMetric;
        private readonly AggregateMetric GetConversationsMetric;
        private readonly AggregateMetric AddMessageMetric;
        private readonly AggregateMetric GetConversationMessagesMetric;
        
        
        public ConversationStoreMetricDecorator(IConversationStore store,IMetricsClient metricsClient)
        {
            this.store = store;
            
            AddConversationMetric = metricsClient.CreateAggregateMetric("AddConversationTime");
            GetConversationsMetric = metricsClient.CreateAggregateMetric("GetConversationsTime");
            AddMessageMetric = metricsClient.CreateAggregateMetric("AddMessageTime");
            GetConversationMessagesMetric = metricsClient.CreateAggregateMetric("GetConversationMessagesTime");
           
        }
        
        public Task<Conversation> AddConversation(Conversation conversation)
        {
            return AddConversationMetric.TrackTime(() => store.AddConversation(conversation));
        }

        public Task<ResultConversations> GetConversations(string userName, string startCt,string endCt, int limit = 50)
        {
            return GetConversationsMetric.TrackTime(() => store.GetConversations(userName, startCt, endCt, limit));
        }

        public Task<Message> AddMessage(string conversationId, Message message)
        {
            return AddMessageMetric.TrackTime(() => store.AddMessage(conversationId, message));
        }

        public Task<ResultMessages> GetConversationMessages(string conversationId,string startCt,string endCt,int limit = 50)
        {
            return GetConversationMessagesMetric.TrackTime(() =>
                store.GetConversationMessages(conversationId, startCt, endCt, limit));
        }
    }
}