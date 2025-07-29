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

        [ObservableProperty]
        string name;

        [ObservableProperty]
        int? quantity;

        [ObservableProperty]
        bool isPurchased;
    }
}