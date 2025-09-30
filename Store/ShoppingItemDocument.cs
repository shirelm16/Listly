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
        public double? Quantity { get; set; }

        [FirestoreProperty("unit")]
        public string? Unit { get; set; }

        [FirestoreProperty("isPurchased")]
        public bool IsPurchased { get; set; }

        [FirestoreProperty("category")]
        public string Category { get; set; }

        [FirestoreProperty("has_priority")]
        public bool HasPriority { get; set; }

        [FirestoreProperty("priority")]
        public string Priority { get; set; }


        public static ShoppingItemDocument FromShoppingItem(ShoppingItem item) => new()
        {
            Id = item.Id.ToString(),
            ShoppingListId = item.ShoppingListId.ToString(),
            Name = item.Name,
            Quantity = item.Quantity,
            Unit = item.Unit,
            IsPurchased = item.IsPurchased,
            Category = item.Category == null ? Model.Category.Other.GetDisplayName() : item.Category.Name.GetDisplayName(),
            HasPriority = item.HasPriority,
            Priority = item.Priority.ToString(),
        };

        public ShoppingItem ToShoppingItem() => new()
        {
            Id = Guid.Parse(Id),
            ShoppingListId = Guid.Parse(ShoppingListId),
            Name = Name,
            Quantity = Quantity,
            Unit = Unit,
            IsPurchased = IsPurchased,
            Category = Category == null ? new ItemCategory() : new ItemCategory(CategoryHelper.FromDisplayName(Category)),
            HasPriority = HasPriority,
            Priority = Priority == null ? Model.Priority.Medium : Enum.GetValues<Priority>().FirstOrDefault(e => e.ToString() == Priority)
        };
    }
}
