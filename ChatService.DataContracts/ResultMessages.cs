using System;
using System.Collections.Generic;
using System.Text;

namespace ChatService.DataContracts
{
    public class ResultMessages
    {
        public ResultMessages(List<Message> messages, string startCt, string endCt)
        {
            StartCt = startCt;
            EndCt = endCt;
            Messages = messages;
        }

        public List<Message> Messages { get; set; }
        public string StartCt { get; set; }
        public string EndCt { get; set; }

    }
}
