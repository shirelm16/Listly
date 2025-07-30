using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Store;
using Listly.View;
using Mopups.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    [QueryProperty(nameof(ShoppingList), "ShoppingList")]
    public partial class ShoppingListDetailsViewModel : BaseViewModel
    {
        private readonly ShoppingItemStore _shoppingItemStore;

        public ShoppingListDetailsViewModel(ShoppingItemStore shoppingItemStore)
        {
            _shoppingItemStore = shoppingItemStore;

            WeakReferenceMessenger.Default.Register<ShoppingItemCreatedMessage>(this, async (r, m) =>
            {
                if (m.Value.ShoppingListId == ShoppingList?.Id)
                {
                    await _shoppingItemStore.AddItemToShoppingList(m.Value);
                    ShoppingList.Items.Add(m.Value);
                }
            });

            WeakReferenceMessenger.Default.Register<ShoppingItemUpdatedMessage>(this, async (r, m) =>
            {
                var item = m.Value;
                await _shoppingItemStore.UpdateShoppingItem(item);
                
                var itemChanged = ShoppingList.Items.First(i => i.Id == item.Id);
                itemChanged.Name = item.Name;
                itemChanged.IsPurchased = item.IsPurchased;
                itemChanged.Quantity = item.Quantity;
            });
        }

        [ObservableProperty]
        ShoppingList shoppingList;


        [RelayCommand]
        async Task EditItem(ShoppingItem shoppingItem)
        {
            var popup = new AddEditShoppingItemPopup("Edit Item", ShoppingList.Id, shoppingItem);
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        async Task DeleteItem(ShoppingItem shoppingItem)
        {
            ShoppingList.Items.Remove(shoppingItem);
            await _shoppingItemStore.RemoveShoppingItem(shoppingItem.Id);
        }

        [RelayCommand]
        async Task AddItem()
        {
            var popup = new AddEditShoppingItemPopup("Add Item", ShoppingList.Id);
            await MopupService.Instance.PushAsync(popup);
        }
    }
}
