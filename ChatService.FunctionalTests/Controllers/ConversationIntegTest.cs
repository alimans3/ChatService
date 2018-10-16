using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ChatService.DataContracts;
using ChatService.FunctionalTests.TestUtils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        private AddMessageDto messageDto1;
        private AddMessageDto messageDto2;
        private AddMessageDto messageDto3;
        private CreateProfileDto userProfile1;
        private CreateProfileDto userProfile2;



        [TestInitialize]
        public void Initialize()
        {
            client = TestMethods.CreateTestServerAndClient();
            
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
            
            messageDto1 = new AddMessageDto("Hi!", conversationDto1.Participants[0]);
            messageDto2 = new AddMessageDto("Hello!", conversationDto1.Participants[1]);
            messageDto3 = new AddMessageDto("Hi!", "foo");
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
            await client.CreateProfile(userProfile1);
            await client.CreateProfile(userProfile2);

            var convs = await client.GetConversations(conversationDto1.Participants[0]);
            
            
            Assert.AreEqual(conversationDto2.Participants[1],convs.Conversations[0].Recipient.Username);
            Assert.AreEqual(conversationDto1.Participants[1],convs.Conversations[1].Recipient.Username);
            Assert.IsTrue(convs.Conversations.Count == 2);
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

            var messages = await client.GetMessages(conv.Id);
            Assert.AreEqual("Hello!", messages.Messages[0].Text);
            Assert.AreEqual(messageDto2.SenderUsername, messages.Messages[0].SenderUsername);
            Assert.AreEqual("Hi!", messages.Messages[1].Text);
            Assert.AreEqual(messageDto1.SenderUsername, messages.Messages[1].SenderUsername);
           
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
