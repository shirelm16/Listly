using Listly.Model;
using Listly.Service;
using Plugin.Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public interface IUsersStore
    {
        public Task CreateOrUpdateUser(User user);
        public Task<UserDocument> GetUser(string userId);
        public Task UpdateDeviceToken(string userId, string token);
    }

    public class FirestoreUsersStore : IUsersStore
    {
        private readonly ICollectionReference _collection;

        public FirestoreUsersStore()
        {
            _collection = CrossFirebaseFirestore.Current.GetCollection("users");
        }

        public async Task UpdateDeviceToken(string userId, string token)
        {
            var userDocRef = _collection.GetDocument(userId);
            var userDoc = (await userDocRef.GetDocumentSnapshotAsync<UserDocument>()).Data;

            if (userDoc != null)
            {
                userDoc.DeviceToken = token;
                await userDocRef.SetDataAsync(userDoc);
            }
        }

        public async Task CreateOrUpdateUser(User user)
        {
            var userDoc = UserDocument.FromUser(user);
            await _collection.GetDocument(user.Id).SetDataAsync(userDoc, SetOptions.Merge());
        }

        public async Task<UserDocument> GetUser(string userId)
        {
            var userDoc = await _collection.GetDocument(userId)
                                .GetDocumentSnapshotAsync<UserDocument>();
            return userDoc.Data;
        }
    }
}
