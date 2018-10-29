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
    public class ProfileController : Controller
    {
        private readonly IProfileStore profileStore;
        private readonly ILogger<ProfileController> logger;
        private readonly AggregateMetric PostProfileMetric;
        private readonly AggregateMetric GetProfileMetric;

        public ProfileController(IProfileStore profileStore,IMetricsClient client, ILogger<ProfileController> logger)
        {
            this.profileStore = profileStore;
            this.logger = logger;
            PostProfileMetric = client.CreateAggregateMetric("PostProfileTime");
            GetProfileMetric = client.CreateAggregateMetric("GetProfileTime");
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateProfile([FromBody] CreateProfileDto request)
        {
            var stopWatch = Stopwatch.StartNew();
            var profile = new UserProfile(request.Username, request.FirstName, request.LastName);
            try
            {
                await profileStore.AddProfile(profile);
                logger.LogInformation(Events.ProfileCreated, "A Profile has been added for user {username}",
                    request.Username);
            }
            catch (StorageErrorException e)
            {
                logger.LogError(Events.StorageError, e, "Failed to create a profile for user {username}", request.Username);
                return StatusCode(503, "Failed to reach storage");
            }
            catch (DuplicateProfileException)
            {
                logger.LogInformation(Events.ProfileAlreadyExists, 
                    "The profile for user {username} cannot be created because it already exists",
                    request.Username);
                return StatusCode(409, "Profile already exists");
            }
            catch (ArgumentException)
            {
                return StatusCode(400, "Invalid or incomplete Request Body");
            }
            catch (Exception e)
            {
                logger.LogError(Events.InternalError, e, "Failed to create a profile for user {username}", request.Username);
                return StatusCode(500, "Failed to create profile");
            }
            finally
            {
                PostProfileMetric.TrackValue(stopWatch.ElapsedMilliseconds);
            }
            return Created(request.Username, profile);
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetProfile(string username)
        {
            var stopWatch = Stopwatch.StartNew();
            try
            {
                UserProfile profile = await profileStore.GetProfile(username);
                return Ok(profile);
            }
            catch (ProfileNotFoundException)
            {
                logger.LogInformation(Events.ProfileNotFound,
                    "A profile was request for user {username} but was not found", username);
                return NotFound();
            }
            catch (StorageErrorException e)
            {
                logger.LogError(Events.StorageError, e, "Failed to retrieve profile of user {username}", username);
                return StatusCode(503, "Failed to reach storage");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occured while retrieving a user profile");
                return StatusCode(500, "Failed to retrieve profile of user {username}");
            }
            finally
            {
                GetProfileMetric.TrackValue(stopWatch.ElapsedMilliseconds);
            }
        }
    }
}
