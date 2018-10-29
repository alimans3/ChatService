using System;
using System.Net;
using System.Threading.Tasks;
using ChatService.Controllers;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
using ChatService.DataContracts;
using ChatService.FunctionalTests.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ChatService.FunctionalTests.Controllers
{
    /// <summary>
    /// In this class we mostly test the edge cases that we cannot test in our integration tests.
    /// It's fine to have overlap between these tests and the controller integrations tests.
    /// </summary>
    [TestClass]
    [TestCategory("UnitTests")]
    public class ProfileControllerTests
    {
        readonly Mock<IProfileStore> profileStoreMock = new Mock<IProfileStore>();
        readonly Mock<ILogger<ProfileController>> loggerMock = new Mock<ILogger<ProfileController>>();
        private IMetricsClient mockClient = TestUtils.GenerateClient();
        private CreateProfileDto createProfileDto = new CreateProfileDto
        {
            Username = "nbilal",
            FirstName = "Nehme",
            LastName = "Bilal"
        };

        [TestMethod]
        public async Task AddProfileReturns503WhenStorageIsUnavailable()
        {
            profileStoreMock.Setup(store => store.AddProfile(It.IsAny<UserProfile>()))
                .ThrowsAsync(new StorageErrorException("Test Failure"));

            var profileController = new ProfileController(profileStoreMock.Object,mockClient, loggerMock.Object);
            IActionResult result = await profileController.CreateProfile(
                createProfileDto);

            TestUtils.AssertStatusCode(HttpStatusCode.ServiceUnavailable, result);
        }

        [TestMethod]
        public async Task AddProfileReturns500WhenUnknownExceptionIsThrown()
        {
            profileStoreMock.Setup(store => store.AddProfile(It.IsAny<UserProfile>()))
                .ThrowsAsync(new UnknownException());

            var profileController = new ProfileController(profileStoreMock.Object,mockClient, loggerMock.Object);
            IActionResult result = await profileController.CreateProfile(createProfileDto);

            TestUtils.AssertStatusCode(HttpStatusCode.InternalServerError, result);
        }

        [TestMethod]
        public async Task GetProfileReturns503WhenStorageIsUnavailable()
        {
            const string username = "username";
            profileStoreMock.Setup(store => store.GetProfile(username))
                .ThrowsAsync(new StorageErrorException("Test Failure"));

            var profileController = new ProfileController(profileStoreMock.Object,mockClient, loggerMock.Object);
            IActionResult result = await profileController.GetProfile(username);

            TestUtils.AssertStatusCode(HttpStatusCode.ServiceUnavailable, result);
        }

        [TestMethod]
        public async Task GetProfileReturns500WhenUnknownExceptionIsThrown()
        {
            const string username = "username";
            profileStoreMock.Setup(store => store.GetProfile(username))
                .ThrowsAsync(new UnknownException());

            var profileController = new ProfileController(profileStoreMock.Object,mockClient, loggerMock.Object);
            IActionResult result = await profileController.GetProfile(username);

            TestUtils.AssertStatusCode(HttpStatusCode.InternalServerError, result);
        }

        // fake exception used for testing internal server error
        private class UnknownException : Exception
        {
        }

    }
}
