using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
using ChatService.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatService.Tests.Storage
{
    [TestClass]
    [TestCategory("Unit")]
    public class InMemoryConversationStoreTest
    {
        private readonly IConversationStore conversationStore = new InMemoryConversationStore();
        private readonly Conversation conversation1 = new Conversation(new List<string> { "amansour", "nbilal" });
        private readonly Conversation conversation2 = new Conversation(new List<string> { "amansour", "foo" });
       
        [TestMethod]
        public async Task AddGetMultipleConversations()
        {
            var conversationReturn1 = await conversationStore.AddConversation(conversation1);
            var conversationReturn2 = await conversationStore.AddConversation(conversation2);
            var conversations = await conversationStore.GetConversations("amansour");
            
            Assert.AreEqual(conversationReturn1.Id,conversations[0].Id);
            Assert.AreEqual(conversationReturn2.Id,conversations[1].Id);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public async Task AddConversationWithNullArgument()
        {
            await conversationStore.AddConversation(null);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public async Task GetConversationsWithNullArgument()
        {
            await conversationStore.GetConversations(null);
        }

        [TestMethod]
        public async Task GetConversationsShouldReturnEmpty()
        {
            CollectionAssert.AreEqual(new List<Conversation>(),await conversationStore.GetConversations("amansour"));
        }

        [TestMethod]
        public async Task AddGetMessage()
        {
            var conversationReturn1 = await conversationStore.AddConversation(conversation1);
            await conversationStore.AddMessage(conversationReturn1.Id, new Message("Hi!", "amansour"));
            var messages = await conversationStore.GetConversationMessages(conversationReturn1.Id);
            Assert.AreEqual("Hi!",messages[0].Text);
            Assert.AreEqual("amansour", messages[0].SenderUsername);
        }

        [TestMethod,ExpectedException(typeof(ArgumentNullException))]
        public async Task AddMessageWithNullMessageShouldThrow()
        {
            var conversationReturn1 = await conversationStore.AddConversation(conversation1);
            await conversationStore.AddMessage(conversationReturn1.Id, null);
        }
        
        [TestMethod,ExpectedException(typeof(InvalidDataException))]
        public async Task AddMessageWithNonExistentUsername()
        {
            var conversationReturn1 = await conversationStore.AddConversation(conversation1);
            await conversationStore.AddMessage(conversationReturn1.Id, new Message("Hi!","foo"));
        }
        
        [TestMethod,ExpectedException(typeof(ArgumentNullException))]
        [DataRow(null,"amansour")]
        [DataRow("Hi!",null)]
        [DataRow("","amansour")]
        [DataRow("Hi!","")]
        public async Task AddMessageWithInvalidArgumentsShouldThrow(string text,string senderUsername)
        {
            var conversationReturn1 = await conversationStore.AddConversation(conversation1);
            await conversationStore.AddMessage(conversationReturn1.Id, new Message(text,senderUsername));
        }
        
        [TestMethod,ExpectedException(typeof(ArgumentNullException))]
        [DataRow(null)]
        [DataRow("")]
        public async Task AddMessageWithNullConversationId(string conversationId)
        {
            var conversationReturn1 = await conversationStore.AddConversation(conversation1);
            await conversationStore.AddMessage(conversationId, new Message("Hi!","amansour"));
        }

        [TestMethod, ExpectedException(typeof(ConversationNotFoundException))]
        public async Task AddMessageWithNonExistingConversationShouldThrow()
        {
            await conversationStore.AddConversation(conversation1);
            await conversationStore.AddMessage("amansour_foo", new Message("Hi!", "amansour"));
        }

        [TestMethod]
        public async Task GetConversationMessagesShouldReturnEmpty()
        {
            var conversationReturn1 = await conversationStore.AddConversation(conversation1);
            CollectionAssert.AreEqual(new List<Message>(),await conversationStore.GetConversationMessages(conversationReturn1.Id));
        }

        [TestMethod]
        public async Task GetMessagesWithNonExistingConversationShouldReturnEmpty()
        {
            await conversationStore.AddConversation(conversation1);
            CollectionAssert.AreEqual(new List<Message>(),await conversationStore.GetConversationMessages("amansour_foo"));
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public async Task GetMessagesWithNullIdShouldThrow()
        {
            await conversationStore.AddConversation(conversation1);
            await conversationStore.GetConversationMessages(null);
        }


    }
}
