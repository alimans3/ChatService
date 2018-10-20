using System;
using System.IO;
using System.Threading.Tasks;
using ChatService.Core;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
using ChatService.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    public class ConversationController : Controller
    {
        private readonly IConversationStore store;
        private readonly ILogger<ConversationController> logger;
        
        public ConversationController(IConversationStore store, ILogger<ConversationController> logger)
        {
            this.store = store;
            this.logger = logger;

        }

        // GET api/conversation/5
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> Get(string conversationId)
        {
            
            try
            {
                var messages = await store.GetConversationMessages(conversationId); 
                var converter = new Converter<Message, GetMessageDto>(message => new GetMessageDto(message));
                var messageDtos = new GetMessagesListDto(messages.ConvertAll(converter));
                logger.LogInformation($"Conversation messages for {conversationId} has been requested!", DateTime.UtcNow);
                return Ok(messageDtos);

            }
            catch (StorageUnavailableException e)
            {
                logger.LogError(Events.StorageError, e,
                    $"Storage was not available to obtain list of conversation messages for {conversationId}",
                    DateTime.UtcNow);
                return StatusCode(503, "Failed to reach Storage");
            }
            catch (Exception e)
            {
                logger.LogError(Events.InternalError, e,
                    $"Failed to obtain list of conversation messages for {conversationId}", DateTime.UtcNow);
                return StatusCode(500, $"Failed to obtain list of conversation messages for {conversationId}");
            }
        }

        // POST api/conversation/5
        [HttpPost("{conversationId}")]
        public async Task<IActionResult> Post(string conversationId, [FromBody]AddMessageDto dto)
        {
            try
            {
                var message = await store.AddMessage(conversationId, new Message(dto));
                logger.LogInformation(Events.MessageAdded,"Message Added Successfully", DateTime.UtcNow);
                var messageDto=new GetMessageDto(message);
                return Ok(messageDto);
            }
            catch (StorageUnavailableException e)
            {
                logger.LogError(Events.StorageError,e, $"Storage was not available to add message", DateTime.UtcNow);
                return StatusCode(503, "Failed to reach Storage");
            }
            catch (ConversationNotFoundException e)
            {
                logger.LogError(Events.ConversationNotFound,e, $"Conversation of Id = {conversationId} was not found", DateTime.UtcNow);
                return NotFound($"Conversation of Id = {conversationId} was not found");
            }
            catch (InvalidDataException e)
            {
                logger.LogError(Events.UsernameNotFound,e, $"Username is not in participants", DateTime.UtcNow);
                return StatusCode(400, "Sender Username doesn't exist in conversation");
            }
            catch(Exception e)
            {
                logger.LogError(Events.InternalError,e, $"Failed to send messages for {conversationId}", DateTime.UtcNow);
                return StatusCode(500, $"Failed to send message");
            }

        }

       
    }
}
