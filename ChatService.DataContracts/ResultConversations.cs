using System;
using System.Collections.Generic;
using System.Text;

namespace ChatService.DataContracts
{
    public class ResultConversations
    {
        public ResultConversations(List<Conversation> conversations,string startCt,string endCt)
        {
            Conversations = conversations;
            StartCt = startCt;
            EndCt = endCt;
        }

        public List<Conversation> Conversations { get;  }
        public string StartCt { get;  }
        public string EndCt { get;  }
    }
}
