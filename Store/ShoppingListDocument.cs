using Google.Cloud.Firestore;
using Java.Util;
using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    [FirestoreData]
    public class ShoppingListDocument
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("owner_id")]
        public string OwnerId { get; set; }

        [FirestoreProperty("collaborators")]
        public List<string> Collaborators { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("lastModified")]
        public Timestamp LastModified { get; set; }

        public static ShoppingListDocument FromShoppingList(ShoppingList list)
        {
            return new ShoppingListDocument
            {
                Id = list.Id.ToString(),
                OwnerId = list.OwnerId.ToString(),
                Name = list.Name,
                LastModified = Timestamp.FromDateTime(list.LastModified.ToUniversalTime())
            };
        }

        public ShoppingList ToShoppingList()
        {
            return new ShoppingList(Name, OwnerId)
            {
                Id = Guid.Parse(Id),
                LastModified = LastModified.ToDateTime()
            };
        }
    }
}
