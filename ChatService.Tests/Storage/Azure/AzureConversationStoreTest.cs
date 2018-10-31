using System.Collections.Generic;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage.Azure;
using ChatService.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;

namespace ChatService.Tests.Storage.Azure
{
    [TestClass]
    [TestCategory("Unit")]
    public class AzureConversationStoreTest
    {
        private Mock<ICloudTable> messageTableMock;
        private Mock<ICloudTable> conversationTableMock;
        private AzureConversationStore store;

        private readonly Conversation testConversation = new Conversation(new List<string> {"amansour", "nbilal"});

        [TestInitialize]
        public void TestInitialize()
        {
            conversationTableMock = new Mock<ICloudTable>();
            messageTableMock = new Mock<ICloudTable>();
            store = new AzureConversationStore(messageTableMock.Object, conversationTableMock.Object);

            conversationTableMock.Setup(m => m.ExecuteBatchAsync(It.IsAny<TableBatchOperation>()))
                .ThrowsAsync(new StorageException(new RequestResult {HttpStatusCode = 503}, "Storage is down", null));
            conversationTableMock.Setup(m =>
                    m.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<UserConversationsTimeRowEntity>>(),
                        It.IsAny<TableContinuationToken>()))
                .ThrowsAsync(
                    new StorageException(new RequestResult {HttpStatusCode = 503}, "Storage is down", null));

            messageTableMock.Setup(m => m.ExecuteAsync(It.IsAny<TableOperation>()))
                .ThrowsAsync(new StorageException(new RequestResult {HttpStatusCode=503},"Storage is down",null));

            messageTableMock.Setup(m =>m.ExecuteQuerySegmentedAsync(It.IsAny<TableQuery<MessagesTableEntity>>(),It.IsAny<TableContinuationToken>()))
               .ThrowsAsync(new StorageException(new RequestResult { HttpStatusCode = 503 }, "Storage is down", null));
        }

        [TestMethod]
        [ExpectedException(typeof(StorageUnavailableException))]
        public async Task AddConversation_StorageUnavailable()
        {
            await store.AddConversation(testConversation);
        }

        [TestMethod]
        [ExpectedException(typeof(StorageUnavailableException))]
        public async Task GetConversations_StorageIsUnavailable()
        {
            await store.GetConversations("amansour",null,null,50);
        }
        
        [TestMethod]
        [ExpectedException(typeof(StorageUnavailableException))]
        public async Task AddDuplicateConversation_StorageInavailable()
        {
            conversationTableMock.Setup(m => m.ExecuteBatchAsync(It.IsAny<TableBatchOperation>())).ThrowsAsync(
                new StorageException(new RequestResult {HttpStatusCode = 409}, "Conflict Conversation", null));
            conversationTableMock.Setup(m => m.ExecuteAsync(It.IsAny<TableOperation>()))
                .Throws(new StorageException(new RequestResult {HttpStatusCode = 503}, "Storage is down", null));
            await store.AddConversation(testConversation);
        }
        
        [TestMethod]
        [ExpectedException(typeof(StorageUnavailableException))]
        public async Task AddMessage_StorageUnavailable()
        {
            conversationTableMock.Setup(m => m.ExecuteAsync(It.IsAny<TableOperation>()))
                .ReturnsAsync(new TableResult {HttpStatusCode = 200});
            var message=new Message("Hola","nbilal");
            await store.AddMessage("foo$%*@nbilal",message);
        }

        [TestMethod]
        [ExpectedException(typeof(StorageUnavailableException))]
        public async Task GetConversationMessages_StorageUnavailable()
        {
            await store.GetConversationMessages("foo",null,null,50);
        }

    }
}