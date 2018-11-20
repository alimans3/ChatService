using System;
using Newtonsoft.Json;

namespace ChatService.DataContracts
{
    public abstract class Notification
    {
        public Notification(string type, DateTime timeStamp)
        {
            Type = type;
            TimeStamp = timeStamp;
        }
        
        public string Type { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}