using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ChatService.Client;
using ChatService.DataContracts;
using ChatService.FunctionalTests.Utils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChatService.FunctionalTests.Controllers
{
    [TestClass]
    [TestCategory("Integration")]
    public class ConversationIntegTest
    {
        private ChatServiceClient client;
        private AddConversationDto conversationDto1;
        private AddConversationDto conversationDto2;
        private AddConversationDto conversationDto3;
        private AddConversationDto conversationDto4;
        private AddMessageDto messageDto1;
        private AddMessageDto messageDto2;
        private AddMessageDto messageDto3;
        private AddMessageDto messageDto4;
        private CreateProfileDto userProfile1;
        private CreateProfileDto userProfile2;
        private CreateProfileDto userProfile3;



        [TestInitialize]
        public void Initialize()
        {
            client = TestUtils.CreateTestServerAndClient();
            
            conversationDto1 = new AddConversationDto
            {
                Participants = new List<string> {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
            };
            conversationDto2 = new AddConversationDto
            {
                Participants = new List<string> {conversationDto1.Participants[0], Guid.NewGuid().ToString()}
            };
            conversationDto3 = new AddConversationDto
            {
                Participants = new List<string> {Guid.NewGuid().ToString(), Guid.NewGuid().ToString()}
            };
            conversationDto4 = new AddConversationDto
            {
                Participants = new List<string> {conversationDto1.Participants[0], Guid.NewGuid().ToString()}
            };
            
            userProfile1 = new CreateProfileDto
            {
                Username = conversationDto1.Participants[1],
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            };
            userProfile2 = new CreateProfileDto
            {
                Username = conversationDto2.Participants[1],
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            };
            userProfile3 = new CreateProfileDto
            {
                Username = conversationDto4.Participants[1],
                FirstName = Guid.NewGuid().ToString(),
                LastName = Guid.NewGuid().ToString()
            };
            
            messageDto1 = new AddMessageDto("Hi!", conversationDto1.Participants[0]);
            messageDto2 = new AddMessageDto("Hello!", conversationDto1.Participants[1]);
            messageDto3 = new AddMessageDto("Hi!", "foo");
            messageDto4 = new AddMessageDto("Kifak!", conversationDto1.Participants[1]);
        }

        [TestMethod]
        public async Task PostConversation()
        {
            var conv = await client.PostConversation(conversationDto1);
            CollectionAssert.AreEquivalent( conversationDto1.Participants, conv.Participants);
            
        }

        [TestMethod]
        public async Task GetConversations()
        {
            await client.PostConversation(conversationDto1);
            await client.PostConversation(conversationDto2);
            await client.PostConversation(conversationDto3);
            await client.PostConversation(conversationDto4);
            await client.CreateProfile(userProfile1);
            await client.CreateProfile(userProfile2);
            await client.CreateProfile(userProfile3);

            var convs = await client.GetConversations(conversationDto1.Participants[0],2);
            Assert.AreEqual(2,convs.Conversations.Count);
            Assert.AreEqual(conversationDto4.Participants[1],convs.Conversations[0].Recipient.Username);
            Assert.AreEqual(conversationDto2.Participants[1],convs.Conversations[1].Recipient.Username);
            
            var prevConvs = await client.GetConversationsFromUri(convs.PreviousUri);
            Assert.AreEqual(1,prevConvs.Conversations.Count);
            Assert.AreEqual(conversationDto1.Participants[1],prevConvs.Conversations[0].Recipient.Username);
            
            var nextConvs = await client.GetConversationsFromUri(prevConvs.NextUri);
            Assert.AreEqual(2,nextConvs.Conversations.Count);
            Assert.AreEqual(conversationDto4.Participants[1],nextConvs.Conversations[0].Recipient.Username);
            Assert.AreEqual(conversationDto2.Participants[1],nextConvs.Conversations[1].Recipient.Username);
            
        }

        [TestMethod]
        public async Task AddMessageToConversation()
        {
            var conv = await client.PostConversation(conversationDto1);

            var message = await client.PostMessage(conv.Id, messageDto1);
            Assert.AreEqual("Hi!",message.Text);
            Assert.AreEqual(messageDto1.SenderUsername, message.SenderUsername);
        }

        [TestMethod]
        public async Task GetConversationMessages()
        {
            var conv = await client.PostConversation(conversationDto1);
            
            await client.PostMessage(conv.Id, messageDto1);
            await client.PostMessage(conv.Id, messageDto2);
            await client.PostMessage(conv.Id, messageDto4);

            
            var messages = await client.GetMessages(conv.Id,2);
            Assert.AreEqual(2,messages.Messages.Count);
            Assert.AreEqual(messageDto4.Text, messages.Messages[0].Text);
            Assert.AreEqual(messageDto4.SenderUsername, messages.Messages[0].SenderUsername);
            Assert.AreEqual(messageDto2.Text, messages.Messages[1].Text);
            Assert.AreEqual(messageDto2.SenderUsername, messages.Messages[1].SenderUsername);
            
            var prevMessages = await client.GetMessagesFromUri(messages.PreviousUri);
            Assert.AreEqual(1,prevMessages.Messages.Count);
            Assert.AreEqual(messageDto1.Text, prevMessages.Messages[0].Text);
            Assert.AreEqual(messageDto1.SenderUsername, prevMessages.Messages[0].SenderUsername);
            
            var nextMessages = await client.GetMessagesFromUri(prevMessages.NextUri);
            Assert.AreEqual(2,nextMessages.Messages.Count);
            Assert.AreEqual(messageDto4.Text, nextMessages.Messages[0].Text);
            Assert.AreEqual(messageDto4.SenderUsername, nextMessages.Messages[0].SenderUsername);
            Assert.AreEqual(messageDto2.Text, nextMessages.Messages[1].Text);
            Assert.AreEqual(messageDto2.SenderUsername, nextMessages.Messages[1].SenderUsername);
            
        }

        [TestMethod]
        public async Task AddMessageSenderNotInConversation()
        {
            var conv = await client.PostConversation(conversationDto1);
            try
            {
                await client.PostMessage(conv.Id, messageDto3);
            }
            catch (ChatServiceException e)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest,e.StatusCode);
            }
            
           
        }
        
        [TestMethod]
        public async Task AddMessageConversationDoesntExist()
        {
            try
            {
                await client.PostMessage(
                    Conversation.GenerateId(new List<string> {messageDto3.SenderUsername, Guid.NewGuid().ToString()}),
                    messageDto3);
            }
            catch (ChatServiceException e)
            {
                Assert.AreEqual(HttpStatusCode.NotFound,e.StatusCode);
            }
            
           
        }


    }
}
