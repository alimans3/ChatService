using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ChatService.Core;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
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
        private readonly AggregateMetric PostConversationMetric;
        private readonly AggregateMetric GetConversationMetric;

        public ConversationsController(IConversationStore store, IMetricsClient client,
            ILogger<ConversationsController> logger, IProfileStore profileStore)
        {
            this.store = store;
            this.logger = logger;
            this.profileStore = profileStore;
            PostConversationMetric = client.CreateAggregateMetric("PostConversationTime");
            GetConversationMetric = client.CreateAggregateMetric("GetConversationTime");

        }

        // GET api/conversations/{username}
        [HttpGet("{username}")]
        public async Task<IActionResult> GetConversations(string username)
        {
            using (logger.BeginScope("This log is for {username}", username))
            {
                var stopWatch = Stopwatch.StartNew();
                try
                {
                    var conversations = await store.GetConversations(username);
                    var converter = new Converter<Conversation, GetConversationsDto>(
                        conversation =>
                        {
                            var recipientUsername = GetConversationsDto.GetRecipient(username, conversation);
                            var profile = profileStore.GetProfile(recipientUsername).Result;
                            return new GetConversationsDto(conversation.Id, profile, conversation.LastModifiedDateUtc);
                        });
                    logger.LogInformation(Events.ConversationsRequested,
                        $"Conversations for {username} has been requested!", DateTime.UtcNow);
                    var conversationsDto = new GetConversationsListDto(conversations.ConvertAll(converter));
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
    }
}
