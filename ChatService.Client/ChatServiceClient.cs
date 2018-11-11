using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChatService.DataContracts;
using Newtonsoft.Json;

namespace ChatService.Client
{

    public class ChatServiceClient : IChatServiceClient
    {
        private readonly HttpClient client;

        public ChatServiceClient(Uri baseUri)
        {
            client = new HttpClient()
            {
                BaseAddress = baseUri
            };
        }

        public ChatServiceClient(HttpClient httpClient)
        {
            this.client = httpClient;
        }

        public async Task CreateProfile(CreateProfileDto profileDto)
        {
            try
            {
                HttpResponseMessage response = await client.PostAsync("api/profile",
                    new StringContent(JsonConvert.SerializeObject(profileDto), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    throw new ChatServiceException("Failed to create user profile", response.ReasonPhrase, response.StatusCode);
                }
            }
            catch (Exception e)
            {
                // make sure we don't catch our own exception we threw above
                if (e is ChatServiceException) throw;

                throw new ChatServiceException("Failed to reach chat service", e, "Internal Server Error",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<UserProfile> GetProfile(string username)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"api/profile/{username}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new ChatServiceException("Failed to retrieve user profile", response.ReasonPhrase, response.StatusCode);
                }

                string content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserProfile>(content);
            }
            catch (JsonException e)
            {
                throw new ChatServiceException($"Failed to deserialize profile for user {username}", e, 
                    "Serialization Exception", HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {
                // make sure we don't catch our own exception we threw above
                if (e is ChatServiceException) throw;

                throw new ChatServiceException("Failed to reach chat service", e, 
                    "Internal Server Error", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GetMessageDto> PostMessage(string id, AddMessageDto messageDto)
        {
            var response = await client.PostAsync($"api/conversation/{id}",
                new StringContent(JsonConvert.SerializeObject(messageDto),
                    Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                throw new ChatServiceException($"Failed to add message {messageDto.Text}",response.ReasonPhrase,response.StatusCode);
            }

            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var message = JsonConvert.DeserializeObject<GetMessageDto>(content);
                return message;
            }
            catch (JsonException e)
            {
                throw new ChatServiceException($"Failed to deserialize message for conversation {id}", e,
                    "Serialization Exception", HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {
                if (e is ChatServiceException) throw;

                throw new ChatServiceException("Failed to reach chat service", e, 
                    "Internal Server Error", HttpStatusCode.InternalServerError);
            }
            
        }
        
        public async Task<GetConversationDto> PostConversation(AddConversationDto conversationDto)
        {
            var response = await client.PostAsync("api/conversations",
                new StringContent(JsonConvert.SerializeObject(conversationDto),
                    Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                throw new ChatServiceException("Failed to add conversation",response.ReasonPhrase,response.StatusCode);
            }
            
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var conversation = JsonConvert.DeserializeObject<GetConversationDto>(content);
                return conversation;
            }
            catch (JsonException e)
            {
                throw new ChatServiceException($"Failed to deserialize conversation", e,
                    "Serialization Exception", HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {
                if (e is ChatServiceException) throw;

                throw new ChatServiceException("Failed to reach chat service", e, 
                    "Internal Server Error", HttpStatusCode.InternalServerError);
            }
            
        }
        
        public async Task<GetConversationsListDto> GetConversations(string username,int limit = 50)
        {
            return await GetConversationsFromUri($"api/conversations/{username}?limit={limit}");
        }

        public async Task<GetConversationsListDto> GetConversationsFromUri(string uri)
        {
            var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                throw new ChatServiceException("Failed to retrieve conversations",response.ReasonPhrase,response.StatusCode);
            }
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var conversation = JsonConvert.DeserializeObject<GetConversationsListDto>(content);
                return conversation;
            }
            catch (JsonException e)
            {
                throw new ChatServiceException($"Failed to deserialize conversations", e,
                    "Serialization Exception", HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {
                if (e is ChatServiceException) throw;

                throw new ChatServiceException("Failed to reach chat service", e, 
                    "Internal Server Error", HttpStatusCode.InternalServerError);
            }

        }
        
        public async Task<GetMessagesListDto> GetMessages(string id,int limit = 50)
        {
            return await GetMessagesFromUri($"api/conversation/{id}?limit={limit}");
        }

        public async Task<GetMessagesListDto> GetMessagesFromUri(string uri)
        {
            var response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                throw new ChatServiceException("Failed to retrieve messages",response.ReasonPhrase,response.StatusCode);
            }
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                var messages = JsonConvert.DeserializeObject<GetMessagesListDto>(content);
                return messages;
            }
            catch (JsonException e)
            {
                throw new ChatServiceException($"Failed to deserialize messages", e,
                    "Serialization Exception", HttpStatusCode.InternalServerError);
            }
            catch (Exception e)
            {
                if (e is ChatServiceException) throw;

                throw new ChatServiceException("Failed to reach chat service", e, 
                    "Internal Server Error", HttpStatusCode.InternalServerError);
            }
        }
        
        
    }
}
