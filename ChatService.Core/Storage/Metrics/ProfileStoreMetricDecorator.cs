using System.Threading.Tasks;
using ChatService.DataContracts;
using Microsoft.Extensions.Logging.Metrics;

namespace ChatService.Core.Storage.Metrics
{
    public class ProfileStoreMetricDecorator : IProfileStore
    {
        private readonly IProfileStore store;
        private readonly AggregateMetric GetProfileMetric;
        private readonly AggregateMetric AddProfileMetric;
        
        public ProfileStoreMetricDecorator(IProfileStore store, IMetricsClient metricsClient)
        {
            this.store = store;
            GetProfileMetric = metricsClient.CreateAggregateMetric("GetProfileTime");
            AddProfileMetric = metricsClient.CreateAggregateMetric("AddProfileTime");
        }
        public Task<UserProfile> GetProfile(string username)
        {
            return GetProfileMetric.TrackTime(() => store.GetProfile(username));
        }

        public Task AddProfile(UserProfile profile)
        {
            return AddProfileMetric.TrackTime(() => store.AddProfile(profile));
        }

        public Task UpdateProfile(UserProfile profile)
        {
            return store.UpdateProfile(profile);
        }
    }
}