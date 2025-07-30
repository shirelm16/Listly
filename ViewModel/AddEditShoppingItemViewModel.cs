using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Store;
using Mopups.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class AddEditShoppingItemViewModel : BaseViewModel
    {
        public Guid ShoppingListId { get; }

        private readonly ShoppingItem _shoppingItem;

        private readonly ShoppingItemStore _shoppingItemStore;

        public AddEditShoppingItemViewModel(ShoppingItemStore shoppingItemStore, ShoppingItem shoppingItem, Guid shoppingListId)
        {
            _shoppingItem = shoppingItem;
            ShoppingListId = shoppingListId;
            _shoppingItemStore = shoppingItemStore;
            Name = shoppingItem?.Name;
            Quantity = shoppingItem?.Quantity;
        }

        [ObservableProperty]
        string name;

        [ObservableProperty]
        int? quantity;

        [RelayCommand]
        void IncreaseQuantity() => Quantity = Quantity != null ? Quantity + 1 : 0;

        [RelayCommand]
        void DecreaseQuantity() => Quantity = Quantity != null ? Math.Max(0, Quantity.Value - 1) : 0;

        [RelayCommand]
        async Task Save()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                if (_shoppingItem == null)
                {
                    var shoppingItem = new ShoppingItem
                    {
                        Name = Name,
                        Id = Guid.NewGuid(),
                        ShoppingListId = ShoppingListId,
                        Quantity = Quantity
                    };
                    await _shoppingItemStore.AddItemToShoppingList(shoppingItem);
                    WeakReferenceMessenger.Default.Send(new ShoppingItemCreatedMessage(shoppingItem));
                }
                else if(_shoppingItem.Name != Name || _shoppingItem.Quantity != Quantity)
                {
                    _shoppingItem.Name = Name;
                    _shoppingItem.Quantity = Quantity;
                    await _shoppingItemStore.UpdateShoppingItem(_shoppingItem);
                    WeakReferenceMessenger.Default.Send(new ShoppingItemUpdatedMessage(_shoppingItem));
                }
            }

            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }
    }
}
