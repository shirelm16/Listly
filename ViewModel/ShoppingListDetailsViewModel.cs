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
    public partial class ShoppingListDetailsViewModel : BaseViewModel, IDisposable
    {
        private readonly IShoppingItemStore _shoppingItemStore;

        public ShoppingListDetailsViewModel(IShoppingItemStore shoppingItemStore)
        {
            _shoppingItemStore = shoppingItemStore;

            WeakReferenceMessenger.Default.Register<ShoppingItemCreatedMessage>(this, async (r, m) =>
            {
                var item = m.Value;
                if (item.ShoppingListId == ShoppingList?.Id)
                {
                    ShoppingList.Items.Add(item);
                    item.PropertyChanged += ShoppingItem_PropertyChanged;
                    WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));
                }
            });

            WeakReferenceMessenger.Default.Register<ShoppingItemUpdatedMessage>(this, async (r, m) =>
            {
                var item = m.Value;
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
            if (shoppingItem == null)
                return;

            var popup = new AddEditShoppingItemPopup(_shoppingItemStore, ShoppingList.Id, shoppingItem);
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        async Task DeleteItem(ShoppingItem shoppingItem)
        {
            if (shoppingItem == null)
                return;

            var confirmed = await Shell.Current.DisplayAlert(
                    "Delete Item",
                    $"Are you sure you want to delete '{shoppingItem.Name}'?",
                    "Delete",
                    "Cancel");

            if (!confirmed)
                return;

            ShoppingList.Items.Remove(shoppingItem);
            await _shoppingItemStore.DeleteAsync(shoppingItem.Id);
            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));
        }

        [RelayCommand]
        async Task AddItem()
        {
            var popup = new AddEditShoppingItemPopup(_shoppingItemStore, ShoppingList.Id);
            await MopupService.Instance.PushAsync(popup);
        }

        partial void OnShoppingListChanged(ShoppingList value)
        {
            if (value?.Items != null)
            {
                foreach (var item in value.Items)
                {
                    item.PropertyChanged += ShoppingItem_PropertyChanged;
                }
            }
        }

        private async void ShoppingItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = sender as ShoppingItem;
            await _shoppingItemStore.UpdateAsync(item);

            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevents the finalizer from running.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unregister all messages from WeakReferenceMessenger
                WeakReferenceMessenger.Default.UnregisterAll(this);

                // Unsubscribe from PropertyChanged events of items in the current ShoppingList
                if (ShoppingList != null)
                {
                    foreach (var item in ShoppingList.Items ?? Enumerable.Empty<ShoppingItem>())
                    {
                        item.PropertyChanged -= ShoppingItem_PropertyChanged;
                    }
                }
            }
        }
    }
}
