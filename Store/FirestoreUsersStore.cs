using Listly.Model;
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
        public Task CreateUser(string userId);
        public Task CreateOrUpdateUser(User user);
        public Task<UserDocument> GetUser(string userId);
        public Task UpdateDeviceToken(string userId, string token);
        public Task SaveUserCategoryOverride(string userId, string category, string normalizedItemName);
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

        public async Task CreateUser(string userId)
        {
            var userDoc = new UserDocument
            {
                Id = userId,
            };
            await _collection.GetDocument(userId).SetDataAsync(userDoc);
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

        public async Task SaveUserCategoryOverride(string userId, string category, string itemName)
        {
            var normalizedName = ItemNameNormalizer.Normalize(itemName);
            var docRef = _collection.GetDocument(userId)
                .GetCollection("categoryOverrides")
                .GetDocument(normalizedName);

            var categoryOverride = new CategoryOverrideDocument
            {
                Category = category,
                LastModifiedUnix = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds()
            };

            await docRef.SetDataAsync(categoryOverride);
        }
    }
}
