using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Store;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Listly.ViewModel
{
    public partial class AddShoppingItemPopupViewModel : BaseViewModel
    {
        private readonly ShoppingItemStore _itemStore;

        public Guid ShoppingListId { get; }

        [ObservableProperty]
        private string itemName;

        [ObservableProperty]
        private int? quantity;

        public AddShoppingItemPopupViewModel(ShoppingItemStore itemStore, Guid shoppingListId)
        {
            _itemStore = itemStore;
            ShoppingListId = shoppingListId;
        }

        [RelayCommand]
        private async Task Add()
        {
            if (string.IsNullOrWhiteSpace(ItemName))
                return;

            var shoppingItem = new ShoppingItem
            {
                Name = ItemName,
                Id = Guid.NewGuid(),
                ShoppingListId = ShoppingListId,
                Quantity = Quantity
            };

            await _itemStore.AddItemToShoppingList(shoppingItem);
            WeakReferenceMessenger.Default.Send(new ShoppingItemCreatedMessage(shoppingItem));
            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }
    }
}
