using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.DataContracts;

namespace ChatService.Core.Storage
{
    public interface IConversationStore
    {
        /// <summary>
        /// Adds the conversation to storage.
        /// </summary>
        /// <exception cref="StorageUnavailableException">If storage cannot be reached</exception>
        /// <exception cref="ArgumentNullException">If the given conversation is null </exception>

        Task<Conversation> AddConversation(Conversation conversation);
        /// <summary>
        /// Gets the conversations of specific username.
        /// </summary>
        /// <returns>list of conversations of a username.</returns>
        /// <exception cref="StorageUnavailableException">If storage cannot be reached</exception>
        /// <exception cref="ArgumentNullException">If the given conversation is null </exception>

        Task<ResultConversations> GetConversations(string userName, string startCt, string endCt, int limit = 50);
        /// <summary>
        /// Adds a message to selected conversation
        /// </summary>
        /// <exception cref="StorageUnavailableException">If storage cannot be reached</exception>
        /// <exception cref="ConversationNotFoundException">If conversation cannot be found</exception>
        /// <exception cref="ArgumentNullException">If the given conversation, message, or message text is null </exception>
        /// <exception cref="InvalidDataException">If the given message username is not in conversation </exception>

        Task<Message> AddMessage(string conversationId, Message message);
        /// <summary>
        /// Gets the conversation messages.
        /// </summary>
        /// <returns>The conversation messages.</returns>
        /// <exception cref="StorageUnavailableException">If storage cannot be reached</exception>
        /// <exception cref="ArgumentNullException">If the given conversation is null </exception>

        Task<ResultMessages> GetConversationMessages(string conversationId,string startCt,string endCt,int limit = 50);


       

    }
}




