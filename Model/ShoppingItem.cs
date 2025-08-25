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
        double? quantity;

        [ObservableProperty]
        string? unit;

        [ObservableProperty]
        bool isPurchased;

        public ShoppingItem() { }

        public ShoppingItem(Guid shoppingListId, string name, double? quantity, string? unit)
        {
            Id = Guid.NewGuid();
            ShoppingListId = shoppingListId;
            Name = name;
            Quantity = quantity;
            Unit = unit;
        }
    }
}