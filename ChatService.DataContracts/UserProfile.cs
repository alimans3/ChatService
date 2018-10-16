using System.Collections.Generic;

namespace ChatService.DataContracts
{
    public class UserProfile
    {
        public UserProfile(string username, string firstname, string lastname)
        {
            Username = username;
            FirstName = firstname;
            LastName = lastname;
        }

        public string Username { get; }
        public string FirstName { get; }
        public string LastName { get; }

        public override bool Equals(object obj)
        {
            var profile = obj as UserProfile;
            return profile != null &&
                   Username == profile.Username &&
                   FirstName == profile.FirstName &&
                   LastName == profile.LastName;
        }

        public override int GetHashCode()
        {
            var hashCode = -1525092123;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Username);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FirstName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LastName);
            return hashCode;
        }
    }
}
