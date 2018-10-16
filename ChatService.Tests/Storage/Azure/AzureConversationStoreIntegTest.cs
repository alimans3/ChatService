using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage.Azure;
using ChatService.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatService.Tests.Storage.Azure
{
    [TestClass]
    [TestCategory("Integration")]
    public class AzureConversationStoreIntegTest
    {
        private string connectionString;
        private AzureConversationStore store;
        private Conversation testConversation;
        private Conversation testConversation1;
        private Conversation testConversation2;
        private Conversation testConversation3;
        private Message testMessage;
        private Message testMessage1;

        [TestInitialize]
        public async Task TestInitialize()
        { 
            connectionString = Environment.GetEnvironmentVariable("connectionString");
            testConversation = new Conversation(new List<string> {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()});
            testMessage = new Message("Hola", testConversation.Participants[1]);
            testConversation1 = new Conversation(new List<string> {testConversation.Participants[0], Guid.NewGuid().ToString()});
            testConversation2 = new Conversation(new List<string> {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()});
            testConversation3 = new Conversation(new List<string>
                {testConversation.Participants[0], testConversation.Participants[1]});
            testMessage1 = new Message("Hi", testConversation.Participants[0]);
            
            var messageTable = new AzureCloudTable(connectionString, "TestMessageTable");
            var userConversationsTable = new AzureCloudTable(connectionString, "TestUserConversationsTable");
            await messageTable.CreateIfNotExistsAsync();
            await userConversationsTable.CreateIfNotExistsAsync();
            store = new AzureConversationStore(messageTable,userConversationsTable);
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
            var conversations = await store.GetConversations(testConversation.Participants[0]);
            Assert.AreEqual(2, conversations.Count);
            CollectionAssert.AreEquivalent(testConversation1.Participants, conversations[0].Participants);
            CollectionAssert.AreEquivalent(testConversation.Participants, conversations[1].Participants);
       
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
            await store.GetConversations(null);
        }

        [TestMethod]
        public async Task AddGetMessage()
        {
            //this assures the order of messages
            await store.AddConversation(testConversation);
            await store.AddMessage(testConversation.Id, testMessage);
            await store.AddMessage(testConversation.Id, testMessage1);
            var messages = await store.GetConversationMessages(testConversation.Id);

            //To delete conversation in cleanup
            testConversation.LastModifiedDateUtc=testMessage1.UtcTime;

            Assert.AreEqual(2, messages.Count);
            Assert.AreEqual(testMessage1.Text, messages[0].Text);
            Assert.AreEqual(testMessage1.SenderUsername, messages[0].SenderUsername);
            Assert.AreEqual(testMessage1.UtcTime, messages[0].UtcTime);
            Assert.AreEqual(testMessage.Text, messages[1].Text);
            Assert.AreEqual(testMessage.SenderUsername, messages[1].SenderUsername);
            Assert.AreEqual(testMessage.UtcTime, messages[1].UtcTime);
            

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
            await store.GetConversationMessages(conversationId);
        }

        [TestMethod]
        public async Task GetConversationMessages_NonExistingConversation()
        {
            var messages = await store.GetConversationMessages("foo");
            Assert.AreEqual(0, messages.Count);
            CollectionAssert.AreEqual(new List<Message>(), messages);
        }

    }
}