using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace Listly.Model
{
    public partial class ShoppingItem : ObservableObject
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public Guid ShoppingListId { get; set; }

        [Ignore]
        public string AddedBy { get; set; }

        [ObservableProperty]
        string name;

        [ObservableProperty]
        int? quantity;

        [ObservableProperty]
        bool isPurchased;

        public ShoppingItem() { }

        public ShoppingItem(Guid shoppingListId, string name, int? quantity)
        {
            Id = Guid.NewGuid();
            ShoppingListId = shoppingListId;
            Name = name;
            Quantity = quantity;
        }
    }
}