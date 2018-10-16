using System;
using System.Threading.Tasks;
using ChatService.Core.Exceptions;
using ChatService.Core.Storage;
using ChatService.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChatService.Tests.Storage
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ProfileStoreTests
    {
        private IProfileStore store = new InMemoryProfileStore();
        private UserProfile testProfile = new UserProfile("nbilal", "Nehme", "Bilal");

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
            await store.GetProfile("nbilal");
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

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddNullProfile()
        {
            await store.AddProfile(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_NullUsername()
        {
            await store.AddProfile(new UserProfile(null, "Nehme", "Bilal"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_EmptyUsername()
        {
            await store.AddProfile(new UserProfile("", "Nehme", "Bilal"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_WhitespaceUsername()
        {
            await store.AddProfile(new UserProfile(" ", "Nehme", "Bilal"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_NullFirstName()
        {
            await store.AddProfile(new UserProfile("nbilal", null, "Bilal"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_EmptyFirstName()
        {
            await store.AddProfile(new UserProfile("nbilal", "", "Bilal"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_WhitespaceFirstName()
        {
            await store.AddProfile(new UserProfile("nbilal", " ", "Bilal"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_NullLastName()
        {
            await store.AddProfile(new UserProfile("nbilal", "Nehme", null));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_EmptyLastName()
        {
            await store.AddProfile(new UserProfile("nbilal", "Nehme", ""));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task AddProfile_WhitespaceLastName()
        {
            await store.AddProfile(new UserProfile("nbilal", "Nehme", " "));
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
