using System;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage.Azure;
using ChatService.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Moq;

namespace ChatService.Tests.Storage.Azure
{
    [TestClass]
    [TestCategory("Integration")]
    public class AzureTableProfileStoreTests
    {
        private Mock<ICloudTable> tableMock;
        private AzureTableProfileStore store;

        private readonly UserProfile testProfile = new UserProfile(Guid.NewGuid().ToString(), "Nehme", "Bilal");

        [TestInitialize]
        public void TestInitialize()
        {
            tableMock = new Mock<ICloudTable>();
            store = new AzureTableProfileStore(tableMock.Object);

            tableMock.Setup(m => m.ExecuteAsync(It.IsAny<TableOperation>()))
                .ThrowsAsync(new StorageException(new RequestResult { HttpStatusCode = 503 }, "Storage is down", null));
        }

        [TestMethod]
        [ExpectedException(typeof(StorageErrorException))]
        public async Task GetProfile_StorageIsUnavailable()
        {
            await store.GetProfile("foo");
        }

        [TestMethod]
        [ExpectedException(typeof(StorageErrorException))]
        public async Task AddProfile_StorageIsUnavailable()
        {
            await store.AddProfile(testProfile);
        }

        [TestMethod]
        [ExpectedException(typeof(StorageErrorException))]
        public async Task UpdateProfile_StorageIsUnavailable()
        {
            await store.UpdateProfile(testProfile);
        }

        [TestMethod]
        [ExpectedException(typeof(StorageErrorException))]
        public async Task TryDelete_StorageIsUnavailable()
        {
            await store.TryDelete("foo");
        }
    }
}