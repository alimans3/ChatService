using System.Threading.Tasks;
using ChatService.DataContracts;

namespace ChatService.Client
{
    public interface IChatServiceClient
    {
        Task CreateProfile(CreateProfileDto profileDto);
        
        /// <param name="id"></param>
        /// <param name="messageDto"></param>
        /// <returns>Http Response and message if response is OK, null if not</returns>
        Task<GetMessageDto> PostMessage(string id, AddMessageDto messageDto);
        
        /// <param name="conversationDto"></param>
        /// <returns>Http Response and conversationDto if response is OK, null if not</returns>
        Task<GetConversationDto> PostConversation(AddConversationDto conversationDto);

        /// <param name="username"></param>
        /// <returns>Http Response and list of conversations if response is OK, null if not</returns>
        Task<GetConversationsListDto> GetConversations(string username,int limit);
        
        /// <param name="id"></param>
        /// <returns>Http Response and list of messages if response is OK, null if not</returns>
        Task<GetMessagesListDto> GetMessages(string id,int limit);
    }
}
