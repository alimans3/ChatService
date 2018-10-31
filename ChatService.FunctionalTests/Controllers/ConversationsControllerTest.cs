using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ChatService.Controllers;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
using ChatService.DataContracts;
using ChatService.FunctionalTests.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChatService.FunctionalTests.Controllers
{
    [TestClass]
    [TestCategory("Unit")]
    public class ConversationsControllerTest
    {
        Mock<IConversationStore> mockStore;
        private Mock<IProfileStore> profileStore;
        Mock<ILogger<ConversationsController>> mockLogger;
        private IMetricsClient mockClient;
        ConversationsController myController;

        [TestInitialize]
        public void Initialize()
        {
            mockStore = new Mock<IConversationStore>();
            mockLogger = new Mock<ILogger<ConversationsController>>();
            profileStore=new Mock<IProfileStore>();
            mockClient = TestUtils.GenerateClient();
            myController = new ConversationsController(mockStore.Object,mockClient, mockLogger.Object,profileStore.Object);
        }

        [TestMethod]
        public async Task GetConversationsReturns503IfStorageUnavailable()
        {
            mockStore.Setup(store => store.GetConversations(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),50))
                     .ThrowsAsync(new StorageUnavailableException("Storage is unavailable!"));
            TestUtils.AssertStatusCode(HttpStatusCode.ServiceUnavailable,
                await myController.GetConversations("amansour", "11", "11", 50));
        }

        [TestMethod]
        public async Task GetConversationsReturns500IfUnknownException()
        {
            mockStore.Setup(store => store.GetConversations(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),50))
                     .ThrowsAsync(new Exception("Unknown Exception!"));
            TestUtils.AssertStatusCode(HttpStatusCode.InternalServerError,
                await myController.GetConversations("amansour", "11", "11", 50));
        }

        [TestMethod]
        public async Task AddConversationReturns503IfStorageUnavailable()
        {
            mockStore.Setup(store => store.AddConversation(It.IsAny<Conversation>()))
                     .ThrowsAsync(new StorageUnavailableException("Storage is unavailable!"));
            AddConversationDto conversationDto = new AddConversationDto
            {
                Participants = new List<string>{ "amansour", "nbilal" } 
            };

            TestUtils.AssertStatusCode(HttpStatusCode.ServiceUnavailable, await myController.AddConversation(conversationDto));
        }

        [TestMethod]
        public async Task AddConversationReturns500IfUnknownException()
        {
            mockStore.Setup(store => store.AddConversation(It.IsAny<Conversation>()))
                     .ThrowsAsync(new Exception("Unknown Exception!"));
            AddConversationDto conversationDto = new AddConversationDto
            {
                Participants = new List<string> { "amansour", "nbilal" }
            };
            TestUtils.AssertStatusCode(HttpStatusCode.InternalServerError, await myController.AddConversation(conversationDto));
        }
    }
}
