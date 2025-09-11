using Listly.Model;
using Plugin.Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public class UserDocument
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("displayName")]
        public string DisplayName { get; set; }

        [FirestoreProperty("email")]
        public string Email { get; set; }

        [FirestoreProperty("deviceTokens")]
        public List<string> DeviceTokens { get; set; }

        public static UserDocument FromUser(User user)
        {
            return new UserDocument()
            {
                Id = user.Id,
                Email = user.Email,
                DeviceTokens = user.DeviceTokens,
                DisplayName = user.Name,
            };
        }
    }
}
