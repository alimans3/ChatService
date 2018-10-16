using System;
using ChatService.Core.Storage;
using ChatService.DataContracts;

namespace ChatService.Core.Utils
{
    public static class ProfileUtils
    {
        public static void Validate(UserProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (string.IsNullOrWhiteSpace(profile.Username))
            {
                throw new ArgumentException($"{nameof(UserProfile.Username)} cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(profile.FirstName))
            {
                throw new ArgumentException($"{nameof(UserProfile.FirstName)} cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(profile.LastName))
            {
                throw new ArgumentException($"{nameof(UserProfile.LastName)} cannot be null or empty");
            }
        }
    }
}