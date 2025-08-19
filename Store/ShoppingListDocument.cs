
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

        public static ShoppingListDocument FromShoppingList(ShoppingList list, string ownerId) => new()
        {
            Id = list.Id.ToString(),
            OwnerId = ownerId,
            Name = list.Name,
            LastModifiedUnix = ((DateTimeOffset)list.LastModified).ToUnixTimeSeconds()
        };

        public ShoppingList ToShoppingList() => new()
        {
            Id = Guid.Parse(Id),
            Name = Name,
            LastModified = DateTimeOffset.FromUnixTimeSeconds(LastModifiedUnix).DateTime
        };
    }
}
