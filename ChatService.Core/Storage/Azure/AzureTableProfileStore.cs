using System;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Utils;
using ChatService.DataContracts;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace ChatService.Core.Storage.Azure
{
    public class AzureTableProfileStore : IProfileStore
    {
        private readonly ICloudTable table;

        public AzureTableProfileStore(ICloudTable cloudTable)
        {
            this.table = cloudTable;
        }

        public async Task<UserProfile> GetProfile(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            var entity = await RetrieveEntity(username);
            return new UserProfile(entity.RowKey, entity.FirstName, entity.LastName);
        }

        public async Task AddProfile(UserProfile profile)
        {
            ValidateArgument(profile);

            var entity = new ProfileTableEntity
            {
                PartitionKey = profile.Username,
                RowKey = profile.Username,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
            };

            var insertOperation = TableOperation.Insert(entity);
            try
            {
                await table.ExecuteAsync(insertOperation);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 409) // not found
                {
                    throw new DuplicateProfileException($"Profile for user {profile.Username} already exists");
                }
                throw new StorageErrorException("Could not write to Azure Table", e);
            }
        }

        public async Task UpdateProfile(UserProfile profile)
        {
            ValidateArgument(profile);

            string username = profile.Username;
            ProfileTableEntity entity = await RetrieveEntity(username);

            entity.FirstName = profile.FirstName;
            entity.LastName = profile.LastName;
            TableOperation updateOperation = TableOperation.Replace(entity);

            try
            {
                await table.ExecuteAsync(updateOperation);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 412) // precondition failed
                {
                    throw new StorageConflictException("Optimistic concurrency failed");
                }
                throw new StorageErrorException($"Could not update profile in storage, username = {username}", e);
            }
        }

        private async Task<ProfileTableEntity> RetrieveEntity(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            TableOperation retrieveOperation = TableOperation.Retrieve<ProfileTableEntity>(partitionKey: username, rowkey: username);

            try
            {
                TableResult tableResult = await table.ExecuteAsync(retrieveOperation);
                var entity = (ProfileTableEntity)tableResult.Result;

                if (entity == null)
                {
                    throw new ProfileNotFoundException($"Could not find a profile for username {username}");
                }

                return entity;
            }
            catch (StorageException e)
            {
                throw new StorageErrorException($"Could not retrieve row for username {username} from storage", e);
            }
        }

        public async Task<bool> TryDelete(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            try
            {
                var entity = await RetrieveEntity(username);

                TableOperation deleteOperation = TableOperation.Delete(entity);
                await table.ExecuteAsync(deleteOperation);
                return true;
            }
            catch (ProfileNotFoundException)
            {
                return false;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 412) // precondition failed
                {
                    throw new StorageConflictException("Optimistic concurrency failed");
                }
                throw new StorageErrorException($"Could not delete profile from storage, username = {username}", e);
            }
        }

        private static void ValidateArgument(UserProfile profile)
        {
            ProfileUtils.Validate(profile);
        }
    }
}
