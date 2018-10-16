using System;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage.Azure;
using ChatService.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatService.Tests.Storage.Azure
{
    [TestClass]
    [TestCategory("Integration")]
    public class AzureTableProfileStoreIntegrationTests
    {
        private const string connectionString = "DefaultEndpointsProtocol=https;AccountName=chatservicestorage1;AccountKey=Ngdwh4ENvLl9tGfEe5E1jK0ISk/hHc7WZf6lhibh32qsffc1InFA7m5yFoNVkcAKsIeDlhzipQJl57imAjNzdA==;EndpointSuffix=core.windows.net";
        private AzureTableProfileStore store;
        private readonly UserProfile testProfile = new UserProfile(Guid.NewGuid().ToString(), "Nehme", "Bilal");

        [TestInitialize]
        public async Task TestInitialize()
        {
            
            var table = new AzureCloudTable(connectionString, "TestTable");
            await table.CreateIfNotExistsAsync();
            store = new AzureTableProfileStore(table);
            
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            await store.TryDelete(testProfile.Username);
        }

        [TestMethod]
        public async Task AddGetProfile()
        {
            await store.AddProfile(testProfile);
            var profile = await store.GetProfile(testProfile.Username);
            Assert.AreEqual(testProfile, profile);
        }

        [TestMethod]
        [ExpectedException(typeof(ProfileNotFoundException))]
        public async Task GetNonExistingProfile()
        {
            await store.GetProfile(testProfile.Username);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task GetProfile_NullUsername()
        {
            await store.GetProfile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(DuplicateProfileException))]
        public async Task AddExistingProfile()
        {
            await store.AddProfile(testProfile);
            await store.AddProfile(testProfile);
        }

        [DataRow("", "Nehme", "Bilal")]
        [DataRow(null, "Nehme", "Bilal")]
        [DataRow("nbilal", "", "Bilal")]
        [DataRow("nbilal", null, "Bilal")]
        [DataRow("nbilal", "Nehme", "")]
        [DataRow("nbilal", "Nehme", null)]
        [TestMethod]
        public async Task AddInvalidProfile(string username, string firstName, string lastName)
        {
            try
            {
                var profile = new UserProfile(username, firstName, lastName);
                await store.AddProfile(profile);

                Assert.Fail($"Expected {nameof(ArgumentException)} was not thrown");
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod]
        public async Task UpdateProfile()
        {
            await store.AddProfile(new UserProfile(testProfile.Username, "Foo", "Bar"));
            await store.UpdateProfile(testProfile);

            Assert.AreEqual(testProfile, await store.GetProfile(testProfile.Username));
        }
    }
}