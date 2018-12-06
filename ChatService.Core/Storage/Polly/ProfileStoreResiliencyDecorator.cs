using ChatService.DataContracts;
using Polly;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace ChatService.Core.Storage.Polly
{
    public class ProfileStoreResiliencyDecorator : IProfileStore
    {
        private readonly IProfileStore profileStore;
        private readonly IResiliencyPolicy resiliencyPolicy;

        public ProfileStoreResiliencyDecorator(IProfileStore profileStore, IResiliencyPolicy resiliencyPolicy)
        {
            this.profileStore = profileStore;
            this.resiliencyPolicy = resiliencyPolicy;
        }

        public async Task AddProfile(UserProfile profile)
        {
             await resiliencyPolicy.ExecuteAsync(() => profileStore.AddProfile(profile));
        }

        public async Task<UserProfile> GetProfile(string username)
        {
            return await resiliencyPolicy.ExecuteAsync(() => profileStore.GetProfile(username));
        }

        public async Task UpdateProfile(UserProfile profile)
        {
            await resiliencyPolicy.ExecuteAsync(() => profileStore.UpdateProfile(profile));
        }
    }
}
