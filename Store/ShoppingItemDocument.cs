using Google.Cloud.Firestore;
using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    [FirestoreData]
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

        [FirestoreProperty("addedBy")]
        public string AddedBy { get; set; }

        public static ShoppingItemDocument FromShoppingItem(ShoppingItem item)
        {
            return new ShoppingItemDocument
            {
                Id = item.Id.ToString(),
                ShoppingListId = item.ShoppingListId.ToString(),
                Name = item.Name,
                Quantity = item.Quantity,
                IsPurchased = item.IsPurchased,
                AddedBy = item.AddedBy
            };
        }

        public ShoppingItem ToShoppingItem()
        {
            return new ShoppingItem(Guid.Parse(ShoppingListId), Name, Quantity)
            {
                Id = Guid.Parse(Id),
                IsPurchased = IsPurchased,
                AddedBy = AddedBy
            };
        }
    }
}
