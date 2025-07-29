using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Store;
using Listly.View;
using Mopups.Services;
using System;
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
                    Items.Add(m.Value);
                    await _shoppingItemStore.AddItemToShoppingList(m.Value);
                }
            });

            WeakReferenceMessenger.Default.Register<ShoppingItemUpdatedMessage>(this, async (r, m) =>
            {
                var item = m.Value;
                await _shoppingItemStore.UpdateShoppingItem(item);
            });
        }

        [ObservableProperty]
        ShoppingList shoppingList;

        public ObservableCollection<ShoppingItem> Items { get; } = new();

        [RelayCommand]
        async Task EditItem(ShoppingItem shoppingItem)
        {
            var popupVm = new EditShoppingItemViewModel(shoppingItem);
            var popup = new EditShoppingItemPopup
            {
                BindingContext = popupVm
            };
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        async Task DeleteItem(ShoppingItem shoppingItem)
        {
            Items.Remove(shoppingItem);
            await _shoppingItemStore.RemoveShoppingItem(shoppingItem.Id);
        }

        partial void OnShoppingListChanged(ShoppingList value)
        {
            Items.Clear();
            foreach (var item in value?.Items ?? Enumerable.Empty<ShoppingItem>())
            {
                item.PropertyChanged += ShoppingItem_PropertyChanged;
                Items.Add(item);
            }
        }

        [RelayCommand]
        async Task ShowAddItemPopup()
        {
            var popupVm = new AddShoppingItemPopupViewModel(ShoppingList.Id);
            var popup = new AddShoppingItemPopup
            {
                BindingContext = popupVm
            };
            await MopupService.Instance.PushAsync(popup);
        }

        private async void ShoppingItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var updatedItem = sender as ShoppingItem;
            if (updatedItem != null)
            {
                await _shoppingItemStore.UpdateShoppingItem(updatedItem);
            }
        }
    }
}
