using System;
using System.Diagnostics;
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

namespace ChatService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationsController : Controller
    {
        private readonly IConversationStore store;
        private readonly ILogger<ConversationsController> logger;
        private readonly IProfileStore profileStore;
        private readonly INotificationService notificationService;
        private readonly AggregateMetric PostConversationMetric;
        private readonly AggregateMetric GetConversationMetric;

        public ConversationsController(IConversationStore store, IMetricsClient client,
            ILogger<ConversationsController> logger, IProfileStore profileStore,INotificationService notificationService)
        {
            this.store = store;
            this.logger = logger;
            this.profileStore = profileStore;
            this.notificationService = notificationService;
            PostConversationMetric = client.CreateAggregateMetric("PostConversationTime");
            GetConversationMetric = client.CreateAggregateMetric("GetConversationTime");

        }

        // GET api/conversations/{username}
        [HttpGet("{username}")]
        public async Task<IActionResult> GetConversations(string username,string startCt,string endCt,int limit = 50)
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                var resultConversations = await store.GetConversations(username,startCt, endCt,limit);
                var converter = new Converter<Conversation, GetConversationsDto>(
                    conversation =>
                    {
                        var recipientUsername = GetConversationsDto.GetRecipient(username, conversation);
                        var profile = profileStore.GetProfile(recipientUsername).Result;
                        return new GetConversationsDto(conversation.Id, profile, conversation.LastModifiedDateUtc);
                    });
               
                logger.LogInformation(Events.ConversationsRequested,
                    $"Conversations for {username} has been requested!", DateTime.UtcNow);
                var nextUri =NextConversationsUri(username, resultConversations.StartCt, limit);
                var previousUri =PreviousConversationsUri(username, resultConversations.EndCt, limit);
                
                var conversationsDto =
                    new GetConversationsListDto(resultConversations.Conversations.ConvertAll(converter), nextUri,
                        previousUri);
                return Ok(conversationsDto);
            }
            catch (StorageUnavailableException e)
            {
                logger.LogError(Events.StorageError, e,
                    $"Storage was not available to obtain list of conversations for {username}", DateTime.UtcNow);
                return StatusCode(503, "Failed to reach Storage");
            }
            catch (Exception e)
            {
                logger.LogError(Events.InternalError, e, $"Failed to obtain list of conversations for {username}",
                    DateTime.UtcNow);
                return StatusCode(500, $"Failed to obtain list of conversations for {username}");
            }
            finally
            {
                GetConversationMetric.TrackValue(stopWatch.ElapsedMilliseconds);
            }
         
            
        }

        // POST api/conversations
        [HttpPost]
        public async Task<IActionResult> AddConversation([FromBody] AddConversationDto conversationDto)
        {
            using (logger.BeginScope("This log is for {conversationId}",
                Conversation.GenerateId(conversationDto.Participants)))
            {
                var stopWatch = Stopwatch.StartNew();
                try
                {
                    var conversation = await store.AddConversation(new Conversation(conversationDto));
                    logger.LogInformation(Events.ConversationCreated,
                        $"Conversation of Participants {conversationDto.Participants[0]} " +
                        $"and {conversationDto.Participants[1]} was created", DateTime.UtcNow);
                    var GetConversationDto = new GetConversationDto(conversation);
                    return Ok(GetConversationDto);
                }
                catch (StorageUnavailableException e)
                {
                    logger.LogError(Events.StorageError, e,
                        "Storage was not available to add conversation of Participants " +
                        $"{conversationDto.Participants[0]} and {conversationDto.Participants[1]}", DateTime.UtcNow);
                    return StatusCode(503, "Failed to reach Storage");
                }
                catch (Exception e)
                {
                    logger.LogError(Events.InternalError, e,
                        $"Failed to add conversation of Participants {conversationDto.Participants[0]} " +
                        $"and {conversationDto.Participants[1]}", DateTime.UtcNow);
                    return StatusCode(500,
                        $"Failed to add conversation of Participants {conversationDto.Participants[0]} " +
                        $"and {conversationDto.Participants[1]}");
                }
                finally
                {
                    PostConversationMetric.TrackValue(stopWatch.ElapsedMilliseconds);
                }
            }
        }
        
        public static string NextConversationsUri(string username,string startCt,int limit)
        {

            if (string.IsNullOrWhiteSpace(startCt))
            {
                return  "";
            }

            return  $"/api/conversations/{username}?startCt={startCt}&limit={limit}";

        }

        public static string PreviousConversationsUri(string username,string endCt,int limit)
        {
            if (string.IsNullOrWhiteSpace(endCt))
            {
                return "";
            }

            return $"/api/conversations/{username}?endCt={endCt}&limit={limit}";
        }
        
        private async Task CreateConversationPayloadAndSend(Conversation conversation)
        {
                var payload = new Payload(Payload.ConversationType,conversation.Id,conversation.LastModifiedDateUtc);
                await notificationService.SendNotification(conversation.Participants[0], payload);
                await notificationService.SendNotification(conversation.Participants[1], payload);
        }
    }
}
