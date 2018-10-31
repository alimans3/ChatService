using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage.Azure;
using ChatService.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatService.Tests.Storage.Azure
{
    [TestClass]
    [TestCategory("Integration")]
    public class AzureConversationStoreIntegTest
    {
        private readonly string connectionString= UnitTestsUtils.GetConnectionStringFromConfig();
        private AzureConversationStore store;
        private Conversation testConversation;
        private Conversation testConversation1;
        private Conversation testConversation2;
        private Conversation testConversation3;
        private Conversation testConversation4;
        private Message testMessage;
        private Message testMessage1;
        private Message testMessage2;
        
        [TestInitialize]
        public async Task TestInitialize()
        {
            testConversation = new Conversation(new List<string> {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()});
            testMessage = new Message("Hola", testConversation.Participants[1]);
            
            testConversation1 = new Conversation(new List<string> {testConversation.Participants[0], Guid.NewGuid().ToString()});
            testConversation2 = new Conversation(new List<string> {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()});
            testConversation3 = new Conversation(new List<string>
                {testConversation.Participants[0], testConversation.Participants[1]});
            testConversation4 = new Conversation(new List<string> {testConversation.Participants[0], Guid.NewGuid().ToString()});
            testMessage1 = new Message("Hi", testConversation.Participants[0]);
            
            var messageTable = new AzureCloudTable(connectionString, "TestMessageTable");
            var userConversationsTable = new AzureCloudTable(connectionString, "TestUserConversationsTable");
            await messageTable.CreateIfNotExistsAsync();
            await userConversationsTable.CreateIfNotExistsAsync();
            store = new AzureConversationStore(messageTable,userConversationsTable);
            testMessage2 = new Message("Hello", testConversation.Participants[1]);
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await store.TryDeleteMessage(testConversation.Id,testMessage);
            await store.TryDeleteMessage(testConversation.Id,testMessage1);
            await store.TryDeleteConversation(testConversation);
            await store.TryDeleteConversation(testConversation1);
            await store.TryDeleteConversation(testConversation2);
            await store.TryDeleteConversation(testConversation3);
        }

        [TestMethod]
        public async Task AddGetConversations()
        {
            //this test assures the order of conversations
            await store.AddConversation(testConversation);
            await store.AddConversation(testConversation1);
            await store.AddConversation(testConversation2);
            await store.AddConversation(testConversation4);
            
            var conversations = await store.GetConversations(testConversation.Participants[0],null,null,2);
            Assert.AreEqual(2, conversations.Conversations.Count);
            CollectionAssert.AreEquivalent(testConversation4.Participants, conversations.Conversations[0].Participants);
            CollectionAssert.AreEquivalent(testConversation1.Participants, conversations.Conversations[1].Participants);

            var PrevConversations =
                await store.GetConversations(testConversation.Participants[0], null, conversations.EndCt, 1);
            Assert.AreEqual(1, PrevConversations.Conversations.Count);
            CollectionAssert.AreEquivalent(testConversation.Participants, PrevConversations.Conversations[0].Participants);

            var PrevNullConversations =
                await store.GetConversations(testConversation.Participants[0], null, PrevConversations.EndCt, 2);
            Assert.AreEqual(0,PrevNullConversations.Conversations.Count);
            Assert.AreEqual(null,PrevNullConversations.StartCt);
            Assert.AreEqual(null,PrevNullConversations.EndCt);
            
            var NextConversations =
                await store.GetConversations(testConversation.Participants[0], PrevConversations.StartCt, null, 2);
            Assert.AreEqual(2, NextConversations.Conversations.Count);
            CollectionAssert.AreEquivalent(testConversation4.Participants, NextConversations.Conversations[0].Participants);
            CollectionAssert.AreEquivalent(testConversation1.Participants, NextConversations.Conversations[1].Participants);

            var NextNullConversations = await store.GetConversations(testConversation.Participants[0],
                NextConversations.StartCt, null, 2);
            Assert.AreEqual(0,NextNullConversations.Conversations.Count);
            Assert.AreEqual(null,NextNullConversations.StartCt);
            Assert.AreEqual(null,NextNullConversations.EndCt);

        }

        [TestMethod]
        public async Task AddExistingConversationShouldReturnConversationFromTable()
        {
            await store.AddConversation(testConversation);
            var conv = await store.AddConversation(testConversation3);
            Assert.AreEqual(testConversation.LastModifiedDateUtc,conv.LastModifiedDateUtc);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddConversation_NullArgument()
        {
            await store.AddConversation(null);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        [DataRow(null,"amansour")]
        [DataRow("amansour",null)]
        public async Task AddConversation_InvalidParticipantsArguments(string participant1,string participant2)
        {
            await store.AddConversation(new Conversation(new List<string>{participant1,participant2}));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task GetConversations_NullArgument()
        {
            await store.GetConversations(null,null,null,50);
        }

        [TestMethod]
        public async Task AddGetMessage()
        {
            //this assures the order of messages
            await store.AddConversation(testConversation);
            await store.AddMessage(testConversation.Id, testMessage);
            await store.AddMessage(testConversation.Id, testMessage1);
            await store.AddMessage(testConversation.Id, testMessage2);
            
            var messages = await store.GetConversationMessages(testConversation.Id,null,null,2);

            //To delete conversation in cleanup
            testConversation.LastModifiedDateUtc=testMessage2.UtcTime;

            Assert.AreEqual(2, messages.Messages.Count);
            Assert.AreEqual(testMessage2, messages.Messages[0]);
            Assert.AreEqual(testMessage1, messages.Messages[1]);
            
            var PrevMessages = await store.GetConversationMessages(testConversation.Id,null,messages.EndCt,1);
            Assert.AreEqual(1, PrevMessages.Messages.Count);
            Assert.AreEqual(testMessage, PrevMessages.Messages[0]);
            
            var PrevNullMessages = await store.GetConversationMessages(testConversation.Id,null,PrevMessages.EndCt,1);
            Assert.AreEqual(0,PrevNullMessages.Messages.Count);
            Assert.AreEqual(null,PrevNullMessages.StartCt);
            Assert.AreEqual(null,PrevNullMessages.EndCt);
            
            var NextMessages = await store.GetConversationMessages(testConversation.Id,PrevMessages.StartCt,null,2);
            Assert.AreEqual(2, NextMessages.Messages.Count);
            Assert.AreEqual(testMessage2, NextMessages.Messages[0]);
            Assert.AreEqual(testMessage1, NextMessages.Messages[1]);
            
            var nextNullMessages = await store.GetConversationMessages(testConversation.Id,NextMessages.StartCt,null,1);
            Assert.AreEqual(0,nextNullMessages.Messages.Count);
            Assert.AreEqual(null,nextNullMessages.StartCt);
            Assert.AreEqual(null,nextNullMessages.EndCt);
            
        }

        [TestMethod]
        [ExpectedException(typeof(ConversationNotFoundException))]
        public async Task AddMessage_ConversationNonExistant()
        {
            await store.AddMessage(testConversation.Id, testMessage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [DataRow("hola","")]
        [DataRow("","nbilal")]
        [DataRow("hola", null)]
        [DataRow(null, "nbilal")]
        public async Task AddMessage_InvalidMessageArgument(string text,string senderUsername)
        {
            await store.AddConversation(testConversation);
            await store.AddMessage(testConversation.Id,new Message(text,senderUsername));

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddMessage_NullMessage()
        {
            await store.AddConversation(testConversation);
            await store.AddMessage(testConversation.Id, null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public async Task AddMessage_SenderNotPartOfConversation()
        {
            Message message = new Message("foo", "halaeddine");
            await store.AddConversation(testConversation);
            await store.AddMessage(testConversation.Id, message);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [DataRow(null)]
        [DataRow("")]
        public async Task GetConversationMessages_NullOrEmptyConversationId(string conversationId)
        {
            await store.GetConversationMessages(conversationId,null,null,50);
        }

        [TestMethod]
        public async Task GetConversationMessages_NonExistingConversation()
        {
            var messages = await store.GetConversationMessages("foo",null,null,50);
            Assert.AreEqual(0, messages.Messages.Count);
        }

    }
}