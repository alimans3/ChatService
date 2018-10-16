using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<List<Conversation>> GetConversations(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new ArgumentNullException(nameof(userName));
            }

            //Define Query
            TableQuery<UserConversationsTimeRowEntity> rangeQuery =
                new TableQuery<UserConversationsTimeRowEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userName),
                        TableOperators.And,
                        TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("RowKey",
                                QueryComparisons.LessThanOrEqual,
                                ConversationUtils.FlipAndConvert(new DateTime(2000, 1, 1))), TableOperators.And,
                            TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual,
                                ConversationUtils.FlipAndConvert(DateTime.UtcNow)))));
            try
            {
                //fetches 100 conversations
                rangeQuery.TakeCount = 100;
                var tableQuerySegment = await userConversationsTable.ExecuteQuerySegmentedAsync(rangeQuery, null);
                var entities = tableQuerySegment.Results;
                var converter = new Converter<UserConversationsTimeRowEntity, Conversation>(entity =>
                    new Conversation(entity.Id, new List<string> {entity.PartitionKey, entity.Recipient},
                        ConversationUtils.ParseDateTime(entity.RowKey)));
                return new List<Conversation>(entities.ConvertAll(converter));
            }
            catch (StorageException)
            {
                throw new StorageUnavailableException("Failed to reach storage!");
            }
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
                RowKey = ConversationUtils.FlipAndConvert(message.UtcTime),
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
                var conversation = new Conversation(conversationId,
                    new List<string> {entity.PartitionKey, entity.Recipient},
                    entity.LastModifiedUtcTime);

                await UpdateConversationTime(conversation, message.UtcTime);
                return message;
            }
            catch (StorageException e)
            {
                throw new StorageUnavailableException($"Failed to reached storage {e.Message}");

            }

        }

        public async Task<List<Message>> GetConversationMessages(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentException(nameof(conversationId));
            }

            //Define Query
            TableQuery<MessagesTableEntity> messageQuery = new TableQuery<MessagesTableEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, conversationId));

            try
            {
                //fetch 50 messages
                messageQuery.TakeCount = 50;
                var result = await messageTable.ExecuteQuerySegmentedAsync(messageQuery, null);
                var entities = result.Results;
                var converter = new Converter<MessagesTableEntity, Message>(entity =>
                    new Message(entity.Text, entity.SenderUsername,
                        ConversationUtils.ParseDateTime(entity.RowKey)));
                var messages = new List<Message>(entities.ConvertAll(converter));
                return messages;
            }
            catch (StorageException)
            {
                throw new StorageUnavailableException("Failed to get messages");
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
                var timeRow = ConversationUtils.FlipAndConvert(conversation.LastModifiedDateUtc);
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
                    ConversationUtils.FlipAndConvert(message.UtcTime)));
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
                RowKey = ConversationUtils.FlipAndConvert(conversation.LastModifiedDateUtc),
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
        private async Task UpdateConversationTime(Conversation conversation, DateTime dateTime)
        {
            var entity1 = await RetrieveConversationByTime(conversation.Participants[0],
                ConversationUtils.FlipAndConvert(conversation.LastModifiedDateUtc));
            var entity2 = await RetrieveConversationByTime(conversation.Participants[1],
                ConversationUtils.FlipAndConvert(conversation.LastModifiedDateUtc));
            var entity3 = await RetrieveConversationById(conversation.Participants[0],
                conversation.Id);
            var entity4 = await RetrieveConversationById(conversation.Participants[1],
                conversation.Id);
            
            var updatedEntity1 = new UserConversationsTimeRowEntity
            {
                Id = entity1.Id, 
                PartitionKey = entity1.PartitionKey, 
                Recipient = entity1.Recipient,
                RowKey = ConversationUtils.FlipAndConvert(dateTime)

            };
            var updatedEntity2 = new UserConversationsTimeRowEntity()
            {
                
                Id = entity2.Id, 
                PartitionKey = entity2.PartitionKey, 
                Recipient = entity2.Recipient,
                RowKey = ConversationUtils.FlipAndConvert(dateTime)

            };
            entity3.LastModifiedUtcTime = dateTime;
            entity4.LastModifiedUtcTime = dateTime;
            
            var operation1 = TableOperation.Delete(entity1);
            var operation2 = TableOperation.Delete(entity2);
            var operation3 = TableOperation.Replace(entity3);
            var operation4 = TableOperation.Replace(entity4);
            var operation5 = TableOperation.Insert(updatedEntity1);
            var operation6 = TableOperation.Insert(updatedEntity2);
            var task1 = userConversationsTable.ExecuteBatchAsync(new TableBatchOperation
            {
                operation1, operation3,operation5
            });
            var task2 = userConversationsTable.ExecuteBatchAsync(new TableBatchOperation
            {
                operation2, operation4,operation6
            });
            await Task.WhenAll(task1, task2);
        }
        
    }
}