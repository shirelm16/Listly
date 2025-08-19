using Listly.Model;
using Plugin.Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public class ShoppingItemDocument
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("shoppingListId")]
        public string ShoppingListId { get; set; }

        [FirestoreProperty("name")]
        public string Name { get; set; }

        [FirestoreProperty("quantity")]
        public int? Quantity { get; set; }

        [FirestoreProperty("isPurchased")]
        public bool IsPurchased { get; set; }


        public static ShoppingItemDocument FromShoppingItem(ShoppingItem item) => new()
        {
            Id = item.Id.ToString(),
            ShoppingListId = item.ShoppingListId.ToString(),
            Name = item.Name,
            Quantity = item.Quantity,
            IsPurchased = item.IsPurchased
        };

        public ShoppingItem ToShoppingItem() => new()
        {
            Id = Guid.Parse(Id),
            ShoppingListId = Guid.Parse(ShoppingListId),
            Name = Name,
            Quantity = Quantity,
            IsPurchased = IsPurchased
        };
    }
}
