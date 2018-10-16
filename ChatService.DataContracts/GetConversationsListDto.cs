using System.Collections.Generic;

namespace ChatService.DataContracts
{
    public class GetConversationsListDto
    {
        public GetConversationsListDto(List<GetConversationsDto> conversations)
        {
            Conversations = conversations;
        }
        
        public List<GetConversationsDto> Conversations{get; set; }
    }
}