
using Listly.Model;
using Plugin.Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public class ShoppingListDocument
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("ownerId")]
        public string OwnerId { get; set; }

        [FirestoreProperty("lastModified")]
        public long LastModifiedUnix { get; set; }

        [FirestoreProperty("shareId")]
        public string ShareId { get; set; }

        [FirestoreProperty("shareExpiresAt")]
        public long? ShareExpiresAtUnix { get; set; }

        [FirestoreProperty("collaborators")]
        public List<string> Collaborators { get; set; }

        public static ShoppingListDocument FromShoppingList(ShoppingList list)
        {
            return new ShoppingListDocument()
            {
                Id = list.Id.ToString(),
                OwnerId = list.OwnerId,
                Name = list.Name,
                LastModifiedUnix = ((DateTimeOffset)list.LastModified).ToUnixTimeSeconds(),
                ShareId = list.ShareId,
                ShareExpiresAtUnix = list.ShareExpiresAt == null ? null : ((DateTimeOffset)list.ShareExpiresAt).ToUnixTimeSeconds(),
                Collaborators = list.Collaborators
            };
        }

        public ShoppingList ToShoppingList() => new()
        {
            Id = Guid.Parse(Id),
            OwnerId = OwnerId,
            Name = Name,
            LastModified = DateTimeOffset.FromUnixTimeSeconds(LastModifiedUnix).DateTime,
            ShareId = ShareId,
            ShareExpiresAt = ShareExpiresAtUnix == null ? null : DateTimeOffset.FromUnixTimeSeconds(ShareExpiresAtUnix.Value).DateTime,
            Collaborators = Collaborators
        };
    }
}
