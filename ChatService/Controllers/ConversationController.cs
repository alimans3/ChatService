using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ChatService.Client;
using ChatService.Core;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
using ChatService.Core.Utils;
using ChatService.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Metrics;
using Newtonsoft.Json;

namespace ChatService.Controllers    
{
    [Route("api/[controller]")]
    public class ConversationController : Controller  
    {
        private readonly IConversationStore store;
        private readonly ILogger<ConversationController> logger;
        private readonly INotificationServiceClient notificationService;
        private readonly AggregateMetric PostMessageMetric;
        private readonly AggregateMetric GetMessagesMetric;

        public ConversationController(IConversationStore store, ILogger<ConversationController> logger,
            IMetricsClient client, INotificationServiceClient notificationService)
        {
            this.store = store;
            this.logger = logger;
            this.notificationService = notificationService;
            PostMessageMetric = client.CreateAggregateMetric("PostMessageTime");
            GetMessagesMetric = client.CreateAggregateMetric("GetMessageTime");

        }

        // GET api/conversation/5
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> Get(string conversationId,string startCt,string endCt,int limit = 50)
        {
            using (logger.BeginScope("This log is for {conversationId}",conversationId))
            {
                var stopWatch = Stopwatch.StartNew();
                try
                {
                    var resultMessages = await store.GetConversationMessages(conversationId,  startCt, endCt, limit);
                    var converter = new Converter<Message, GetMessageDto>(message => new GetMessageDto(message));
                    var nextUri =NextMessagesUri(conversationId, resultMessages.StartCt, limit);
                    var previousUri =PreviousMessagesUri(conversationId, resultMessages.EndCt, limit);
                    
                    var messageDtos = new GetMessagesListDto(resultMessages.Messages.ConvertAll(converter), nextUri,
                        previousUri);
                    logger.LogInformation(Events.MessagesRequested,
                        $"Conversation messages for {conversationId} has been requested!", DateTime.UtcNow);
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
                finally
                {
                    GetMessagesMetric.TrackValue(stopWatch.ElapsedMilliseconds);
                }
            }
        }

        // POST api/conversation/5
        [HttpPost("{conversationId}")]
        public async Task<IActionResult> Post(string conversationId, [FromBody]AddMessageDto dto)
        {
            using (logger.BeginScope("This log is for {conversationId}",conversationId))
            {
                var stopWatch = Stopwatch.StartNew();
                try
                {
                    var message = await store.AddMessage(conversationId, new Message(dto));
                    await CreateMessagePayloadAndSend(conversationId, message);
                    logger.LogInformation(Events.MessageAdded, "Message Added Successfully", DateTime.UtcNow);
                    var messageDto = new GetMessageDto(message);
                    return Ok(messageDto);
                }
                catch (StorageUnavailableException e)
                {
                    logger.LogError(Events.StorageError, e, $"Storage was not available to add message",
                        DateTime.UtcNow);
                    return StatusCode(503, "Failed to reach Storage");
                }
                catch (ConversationNotFoundException e)
                {
                    logger.LogError(Events.ConversationNotFound, e,
                        $"Conversation of Id = {conversationId} was not found", DateTime.UtcNow);
                    return NotFound($"Conversation of Id = {conversationId} was not found");
                }
                catch (InvalidDataException e)
                {
                    logger.LogError(Events.UsernameNotFound, e, $"Username is not in participants", DateTime.UtcNow);
                    return StatusCode(400, "Sender Username doesn't exist in conversation");
                }
                catch (Exception e)
                {
                    logger.LogError(Events.InternalError, e, $"Failed to send messages for {conversationId}",
                        DateTime.UtcNow);
                    return StatusCode(500, $"Failed to send message");
                }
                finally
                {
                    PostMessageMetric.TrackValue((double) stopWatch.ElapsedMilliseconds);
                }
            }

        }
        
        public static string NextMessagesUri(string conversationId, string startCt, int limit)
        {
            if (string.IsNullOrWhiteSpace(startCt))
            {
                return "";
            }

            return $"/api/conversation/{conversationId}?startCt={startCt}&limit={limit}";
        }

        public static string PreviousMessagesUri(string conversationId, string endCt, int limit)
        {
            if (string.IsNullOrWhiteSpace(endCt))
            {
                return "";
            }

            return $"/api/conversation/{conversationId}?endCt={endCt}&limit={limit}";
        }

        private async Task CreateMessagePayloadAndSend(string conversationId,Message message)
        {
            var notification = new MessageAddedNotification(conversationId,message.UtcTime);
            var participants = Conversation.ParseId(conversationId);
            var notificationDto = new NotificationDto(participants,JsonConvert.SerializeObject(notification));
            await notificationService.SendNotification(notificationDto);
        }
    }
}
