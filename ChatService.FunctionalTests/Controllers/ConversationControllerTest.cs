﻿using System;
using System.Net;
using System.Threading.Tasks;
using ChatService.Controllers;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
using ChatService.DataContracts;
using ChatService.FunctionalTests.TestUtils;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChatService.FunctionalTests.Controllers
{
    [TestClass]
    [TestCategory("Unit")]
    public class ConversationControllerTest
    {
        Mock<IConversationStore> mockStore;
        Mock<ILogger<ConversationController>> mockLogger;
        ConversationController myController;

        [TestInitialize]
        public void Initialize()
        {
            mockStore = new Mock<IConversationStore>();
            mockLogger = new Mock<ILogger<ConversationController>>();
            myController = new ConversationController(mockStore.Object, mockLogger.Object);
        }

        [TestMethod]
        public async Task GetConversationMessagesReturns503IfStorageUnavailable()
        {
            mockStore.Setup(store => store.GetConversationMessages(It.IsAny<string>()))
                     .ThrowsAsync(new StorageUnavailableException("Storage is unavailable!"));
            TestMethods.AssertStatusCode(HttpStatusCode.ServiceUnavailable, await myController.Get("amansour_nbilal"));
        }

        [TestMethod]
        public async Task GetConversationMessagesReturns500IfUnknownException()
        {
            mockStore.Setup(store => store.GetConversationMessages(It.IsAny<string>()))
                     .ThrowsAsync(new Exception("Unknown Exception!"));
            TestMethods.AssertStatusCode(HttpStatusCode.InternalServerError, await myController.Get("amansour_nbilal"));
        }

        [TestMethod]
        public async Task AddMessageReturns503IfStorageUnavailable()
        {
            mockStore.Setup(store => store.AddMessage(It.IsAny<string>(), It.IsAny<Message>()))
                     .ThrowsAsync(new StorageUnavailableException("Storage is unavailable!"));
            var messageDto = new AddMessageDto("Hi!", "amansour");
            
            TestMethods.AssertStatusCode(HttpStatusCode.ServiceUnavailable, await myController.Post("amansour_nbilal",messageDto));
        }

        [TestMethod]
        public async Task AddMessageReturns500IfUnknownException()
        {
            mockStore.Setup(store => store.AddMessage(It.IsAny<string>(), It.IsAny<Message>()))
                     .ThrowsAsync(new Exception("Unknown Exception!"));
            AddMessageDto messageDto = new AddMessageDto("Hi!", "amansour");

            TestMethods.AssertStatusCode(HttpStatusCode.InternalServerError, await myController.Post("amansour_nbilal", messageDto));
        }
    }
}