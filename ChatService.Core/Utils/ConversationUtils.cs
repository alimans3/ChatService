using System;

namespace ChatService.Core.Utils
{
    public class ConversationUtils
    {
        
        /// <summary>
        /// flips the time and converts it to a string
        /// </summary>
        /// <param name="dateTime"></param>
        public static string FlipAndConvert(DateTime dateTime)
        {
            return (DateTime.MaxValue.Ticks - dateTime.Ticks).ToString("d19");
        }
        
        /// <summary>
        /// takes an inverted ticks string and returns corresponding datetime
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime ParseDateTime(string dateTime)
        {
            return new DateTime(DateTime.MaxValue.Ticks - long.Parse(dateTime));
        }
    }
}