using System;
using System.Collections.Generic;

namespace ChatService.DataContracts
{
    public class GetMessagesListDto
    {
        public GetMessagesListDto(List<GetMessageDto> messages,string nextUri,string previousUri)
        {
            Messages = messages;
            NextUri = nextUri;
            PreviousUri = previousUri;
        }
        public List<GetMessageDto> Messages { get;  }
        public string NextUri { get;  }
        public string PreviousUri { get;  }
    }
}