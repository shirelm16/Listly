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

        [FirestoreProperty("deviceToken")]
        public string DeviceToken { get; set; }

        public static UserDocument FromUser(User user)
        {
            var userDoc =  new UserDocument()
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.Name,
                DeviceToken = user.DeviceToken,
            };

            return userDoc;
        }
    }
}
