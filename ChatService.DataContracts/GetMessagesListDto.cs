using System.Collections.Generic;

namespace ChatService.DataContracts
{
    public class GetMessagesListDto
    {
        public GetMessagesListDto(List<GetMessageDto> messages)
        {
            Messages = messages;
        }
        public List<GetMessageDto> Messages { get; set; }
    }
}