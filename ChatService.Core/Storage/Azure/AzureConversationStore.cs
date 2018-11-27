using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Utils;
using ChatService.DataContracts;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ChatService.Core.Storage.Azure
{
    public class AzureConversationStore : IConversationStore
    {
        private readonly ICloudTable messageTable;
        private readonly ICloudTable userConversationsTable;

        public AzureConversationStore(ICloudTable messageTable, ICloudTable userConversationsTable)
        {
            this.messageTable = messageTable;
            this.userConversationsTable = userConversationsTable;
        }

        public async Task<Conversation> AddConversation(Conversation conversation)
        {
            if (conversation == null)
            {
                throw new ArgumentNullException(nameof(conversation));
            }

            if (string.IsNullOrWhiteSpace(conversation.Participants[0]) ||
                string.IsNullOrWhiteSpace(conversation.Participants[1]))
            {
                throw new ArgumentNullException(nameof(conversation.Participants));
            }

            //Execute Transactions
            try
            {
                var task1 = AddConversationForUser(conversation.Participants[0], conversation.Participants[1], conversation);
                var task2 = AddConversationForUser(conversation.Participants[1], conversation.Participants[0], conversation);
                await Task.WhenAll(task1, task2);
                return conversation;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode != 409)
                {
                    throw new StorageUnavailableException("Failed to reach storage!");
                }

                //return existing conversation in store in case of conflict
                try
                {
                    var entity = await RetrieveConversationById(conversation.Participants[0], conversation.Id);
                    return new Conversation(entity.RowKey, new List<string> {entity.Recipient, entity.PartitionKey},
                        entity.LastModifiedUtcTime);
                }
                catch (StorageException)
                {
                    throw new StorageUnavailableException("Failed to reach storage!");
                }
            }



        }

        public async Task<ResultConversations> GetConversations(string userName,string startCt,string endCt,int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            ResultConversations result;
            
            if (!string.IsNullOrWhiteSpace(endCt))
            {
                result = await GetConversationsUntilEnd(userName, endCt, limit);
            }
            else if (!string.IsNullOrWhiteSpace(startCt))
            {
                result = await GetConversationsFromStart(userName, startCt, limit);
            }
            else
            {
                result = await GetMostRecentConversations(userName, limit);
            }

            return result;
        }

        public async Task<Message> AddMessage(string conversationId, Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.SenderUsername))
            {
                throw new ArgumentException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.Text))
            {
                throw new ArgumentException(nameof(message));
            }


            MessagesTableEntity messageEntity = new MessagesTableEntity
            {
                PartitionKey = conversationId,
                RowKey = ConversationUtils.DateTimeToRowKey(message.UtcTime),
                Text = message.Text,
                SenderUsername = message.SenderUsername

            };

            //Define operations
            var insertOperation = TableOperation.Insert(messageEntity);
            var retrieveOperation =
                TableOperation.Retrieve<UserConversationsIdRowEntity>(message.SenderUsername, conversationId);

            try
            {
                var result = await userConversationsTable.ExecuteAsync(retrieveOperation);
                if (result.HttpStatusCode == 404)
                {
                    var participantsFromId = Conversation.ParseId(conversationId);
                    if (!participantsFromId.Contains(message.SenderUsername))
                    {
                        throw new InvalidDataException("Sender not part of conversation");
                    }

                    throw new ConversationNotFoundException("Conversation not found");
                }

                await messageTable.ExecuteAsync(insertOperation);
                var entity = (UserConversationsIdRowEntity) result.Result;
                
                retrieveOperation =
                    TableOperation.Retrieve<UserConversationsIdRowEntity>(entity.Recipient, conversationId);
                var retrieveResult2 = await userConversationsTable.ExecuteAsync(retrieveOperation);
                
                var entity2 = (UserConversationsIdRowEntity) retrieveResult2.Result;

                await UpdateConversationTime(entity2.RowKey,entity2.PartitionKey,entity2.LastModifiedUtcTime, message.UtcTime);
                await UpdateConversationTime(entity.RowKey,entity.PartitionKey,entity.LastModifiedUtcTime, message.UtcTime);
                return message;
            }
            catch (StorageException e)
            {
                throw new StorageUnavailableException($"Failed to reached storage {e.Message}");

            }

        }
        
        public async Task<ResultMessages> GetConversationMessages(string conversationId,string startCt,string endCt,int limit = 50)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentException(nameof(conversationId));
            }

            ResultMessages result;
            if (!string.IsNullOrWhiteSpace(endCt))
            {
                result = await GetConversationMessagesUntilEnd(conversationId, endCt, limit);
            }
            else if (!string.IsNullOrWhiteSpace(startCt))
            {
                result = await GetConversationMessagesFromStart(conversationId, startCt, limit);
            }
            else
            {
                result = await GetMostRecentMessages(conversationId, limit);
            }

           return result;
        }

        private async Task<ResultMessages> GetMostRecentMessages(string conversationId, int limit)
        {
            //Define Query
            TableQuery<MessagesTableEntity> messageQuery = new TableQuery<MessagesTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, conversationId),
                    TableOperators.And,
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan,
                            ConversationUtils.DateTimeToRowKey(new DateTime(2000, 1, 1))), TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual,
                            ConversationUtils.DateTimeToRowKey(DateTime.UtcNow)))));

            messageQuery.TakeCount = limit;
            return await ExecuteMessageQueryAndConvert(messageQuery);
        }

        private async Task<ResultMessages> GetConversationMessagesFromStart(string conversationId, string startCt, int limit)
        {
            //Define Query
            TableQuery<MessagesTableEntity> messageQuery = new TableQuery<MessagesTableEntity>().Where(TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, conversationId),
                TableOperators.And, TableQuery.GenerateFilterCondition("RowKey",QueryComparisons.LessThan,startCt)));

            messageQuery.TakeCount = limit;

            return await ExecuteMessageQueryAndConvert(messageQuery);
        }

        private async Task<ResultMessages> GetConversationMessagesUntilEnd(string conversationId, string endCt,
            int limit)
        {
            //Define Query
            TableQuery<MessagesTableEntity> messageQuery = new TableQuery<MessagesTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, conversationId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, endCt)));
            
            messageQuery.TakeCount = limit;
            return await ExecuteMessageQueryAndConvert(messageQuery);
        }
        
        private async Task<ResultMessages> ExecuteMessageQueryAndConvert(TableQuery<MessagesTableEntity> rangeQuery)
        {
            try
            {
                var resultEntities = await messageTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
                var entities = resultEntities.Results;
                var converter = new Converter<MessagesTableEntity, Message>(entity =>
                    new Message(entity.Text, entity.SenderUsername,
                        ConversationUtils.ParseDateTime(entity.RowKey)));
                var messages = new List<Message>(entities.ConvertAll(converter));
                if (messages.Count == 0)
                {
                    return new ResultMessages(messages, null, null);
                }

                return new ResultMessages(messages, ConversationUtils.DateTimeToRowKey(messages.First().UtcTime),
                    ConversationUtils.DateTimeToRowKey(messages.Last().UtcTime));
            }
            catch (StorageException)
            {
                throw new StorageUnavailableException("Failed to get messages");
            }
        }

        private async Task<ResultConversations> GetMostRecentConversations(string username, int limit)
        {
            //Define Query
            TableQuery<UserConversationsTimeRowEntity> rangeQuery =
                new TableQuery<UserConversationsTimeRowEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, username),
                        TableOperators.And, TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan,
                                ConversationUtils.DateTimeToRowKey(new DateTime(2000, 1, 1))),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual,
                                ConversationUtils.DateTimeToRowKey(DateTime.UtcNow)))));

            //fetches ${limit} conversations
            rangeQuery.TakeCount = limit;
            return await ExecuteConversationQueryAndConvert(rangeQuery);
        }

        private async Task<ResultConversations> GetConversationsFromStart(string username, string startCt, int limit)
        {
            //Define Query
            TableQuery<UserConversationsTimeRowEntity> rangeQuery =
                new TableQuery<UserConversationsTimeRowEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, username),
                        TableOperators.And, TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, startCt),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual,
                                ConversationUtils.DateTimeToRowKey(DateTime.UtcNow)))));

            rangeQuery.TakeCount = limit;
            return await ExecuteConversationQueryAndConvert(rangeQuery);

        }

        private async Task<ResultConversations> GetConversationsUntilEnd(string username, string endCt, int limit)
        {
            //Define Query
            TableQuery<UserConversationsTimeRowEntity> rangeQuery =
                new TableQuery<UserConversationsTimeRowEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, username),
                        TableOperators.And, TableQuery.CombineFilters(
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, endCt),
                            TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual,
                                ConversationUtils.DateTimeToRowKey(new DateTime(2000, 1, 1))))));

            rangeQuery.TakeCount = limit;
            return await ExecuteConversationQueryAndConvert(rangeQuery);
        }

        private async Task<ResultConversations> ExecuteConversationQueryAndConvert(
            TableQuery<UserConversationsTimeRowEntity> rangeQuery)
        {
            try
            {
                var tableQuerySegment = await userConversationsTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
                var entities = tableQuerySegment.Results;
                var converter = new Converter<UserConversationsTimeRowEntity, Conversation>(entity =>
                    new Conversation(entity.Id, new List<string> {entity.PartitionKey, entity.Recipient},
                        ConversationUtils.ParseDateTime(entity.RowKey)));
                var conversations = new List<Conversation>(entities.ConvertAll(converter));

                if (conversations.Count == 0)
                {
                    return new ResultConversations(conversations, null, null);
                }

                return  new ResultConversations(conversations,
                    ConversationUtils.DateTimeToRowKey(conversations.First().LastModifiedDateUtc),
                    ConversationUtils.DateTimeToRowKey(conversations.Last().LastModifiedDateUtc));

            }
            catch (StorageException)
            {
                throw new StorageUnavailableException("Failed to reach storage!");
            }
        }



        //Helping Functions
        /// <summary>
        /// Deletes 4 entities corresponding to a conversation
        /// </summary>
        /// <param name="conversation"></param>
        public async Task<bool> TryDeleteConversation(Conversation conversation)
        {
            try
            {
                var timeRow = ConversationUtils.DateTimeToRowKey(conversation.LastModifiedDateUtc);
                var task1 = RetrieveAndDelete(conversation.Participants[0], conversation.Id, timeRow);
                var task2 = RetrieveAndDelete(conversation.Participants[1], conversation.Id, timeRow);
                await Task.WhenAll(task1, task2);
            }
            catch (StorageException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Deletes entity corresponding to a message
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="message"></param>
        public async Task<bool> TryDeleteMessage(string conversationId, Message message)
        {
            try
            {
                var entity = await messageTable.ExecuteAsync(TableOperation.Retrieve<MessagesTableEntity>(
                    conversationId,
                    ConversationUtils.DateTimeToRowKey(message.UtcTime)));
                await messageTable.ExecuteAsync(TableOperation.Delete((MessagesTableEntity) entity.Result));
                return true;
            }
            catch (StorageException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
        }

        /// <summary>
        /// Adds 2 conversation entities for 1 participant in conversation
        /// </summary>
        /// <param name="username"></param>
        /// <param name="recipient"></param>
        /// <param name="conversation"></param>
        private async Task AddConversationForUser(string username, string recipient, Conversation conversation)
        {
            //Create Entities
            var entity1Id = new UserConversationsIdRowEntity
            {
                PartitionKey = username,
                RowKey = conversation.Id,
                LastModifiedUtcTime = conversation.LastModifiedDateUtc,
                Recipient = recipient
            };

            var entity1Time = new UserConversationsTimeRowEntity
            {
                PartitionKey = username,
                RowKey = ConversationUtils.DateTimeToRowKey(conversation.LastModifiedDateUtc),
                Recipient = recipient,
                Id = conversation.Id

            };
            
            //Define Operations
            var operation1 = TableOperation.Insert(entity1Id);
            var operation2 = TableOperation.Insert(entity1Time);
            
            //Define Transaction
            var transaction = new TableBatchOperation {operation1, operation2};
            
            //Execute Transaction
            await userConversationsTable.ExecuteBatchAsync(transaction);
            
        }

        /// <summary>
        /// Retrieves a conversation entity of Id row key of 1 participant.
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="rowKey"></param>
        private async Task<UserConversationsIdRowEntity> RetrieveConversationById(string partitionKey, string rowKey)
        {
            var operationRetrieve =
                TableOperation.Retrieve<UserConversationsIdRowEntity>(partitionKey, rowKey);
            var result = await userConversationsTable.ExecuteAsync(operationRetrieve);
            return (UserConversationsIdRowEntity) result.Result;
        }
        
        /// <summary>
        /// Retrieves a conversation entity of time row key of 1 participant.
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="rowKey"></param>
        private async Task<UserConversationsTimeRowEntity> RetrieveConversationByTime(string partitionKey, string rowKey)
        {
            var operationRetrieve =
                TableOperation.Retrieve<UserConversationsTimeRowEntity>(partitionKey, rowKey);
            var result = await userConversationsTable.ExecuteAsync(operationRetrieve);
            return (UserConversationsTimeRowEntity) result.Result;
        }

        /// <summary>
        /// Retrieves 2 entities corresponding to a participant in a conversation and deletes them.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="Id"></param>
        /// <param name="timeRowKey"></param>
        private async Task RetrieveAndDelete(string username, string Id, string timeRowKey)
        {
            var entity1 = await RetrieveConversationById(username, Id);
            var entity2 = await RetrieveConversationByTime(username, timeRowKey);
            await Task.WhenAll(new List<Task>
            {
                userConversationsTable.ExecuteAsync(TableOperation.Delete(entity1)),
                userConversationsTable.ExecuteAsync(TableOperation.Delete(entity2))
            });
            
        }

        /// <summary>
        /// Updates a conversation time.
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private async Task UpdateConversationTime(string conversationId, string senderUsername, DateTime currentDateTime , DateTime dateTime)
        {
            var entity1 = await RetrieveConversationByTime(senderUsername, ConversationUtils.DateTimeToRowKey(currentDateTime));
            var entity2 = await RetrieveConversationById(senderUsername,conversationId);
            
            var updatedEntity = new UserConversationsTimeRowEntity
            {
                Id = entity1.Id, 
                PartitionKey = entity1.PartitionKey, 
                Recipient = entity1.Recipient,
                RowKey = ConversationUtils.DateTimeToRowKey(dateTime)

            };
            
            entity2.LastModifiedUtcTime = dateTime;
            
            var operation1 = TableOperation.Delete(entity1);
            var operation2 = TableOperation.Replace(entity2);
            var operation3 = TableOperation.Insert(updatedEntity);
            
            await userConversationsTable.ExecuteBatchAsync(new TableBatchOperation
            {
                operation1, operation2,operation3
            });
        }
        
    }
}