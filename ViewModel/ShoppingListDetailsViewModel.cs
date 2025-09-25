using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Services;
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
        private readonly IShoppingItemsSortingService _sortingItemsService;

        public ShoppingListDetailsViewModel(IShoppingItemStore shoppingItemStore, IShoppingItemsSortingService sortingItemsService)
        {
            _shoppingItemStore = shoppingItemStore;
            _sortingItemsService = sortingItemsService;

            WeakReferenceMessenger.Default.Register<ShoppingItemCreatedMessage>(this, async (r, m) =>
            {
                var item = m.Value;
                if (item.ShoppingListId == ShoppingList?.Id)
                {
                    ShoppingList.Items.Add(item);
                    item.PropertyChanged += ShoppingItem_PropertyChanged;
                    item.ItemPurchased += ShoppingItem_OnItemPurchased;
                    item.ItemUnpurchased += ShoppingItem_OnItemUnpurchased;
                    item.CategoryChanged += ShoppingItem_OnCategoryChanged;
                    UpdateItemCollections();
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
                itemChanged.Category = item.Category;
            });
        }

        [ObservableProperty]
        ShoppingList shoppingList;

        [ObservableProperty]
        private ShoppingItem? lastPurchasedItem;

        [ObservableProperty]
        private bool showUndoToast;

        private CancellationTokenSource? _hideToastCancellation;

        [ObservableProperty]
        private ObservableCollection<ShoppingItem> activeItems = new();

        [ObservableProperty]
        private ObservableCollection<ShoppingItem> purchasedItems = new();

        [ObservableProperty]
        private bool isPurchasedSectionExpanded = false;

        [ObservableProperty]
        private string purchasedSectionTitle = "Purchased (0 items)";

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
            await _shoppingItemStore.DeleteShoppingItemAsync(shoppingItem.ShoppingListId, shoppingItem.Id);
            UpdateItemCollections();
            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));
        }

        [RelayCommand]
        async Task AddItem()
        {
            var popup = new AddEditShoppingItemPopup(_shoppingItemStore, ShoppingList.Id);
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        private void UndoLastPurchase()
        {
            if (LastPurchasedItem != null)
            {
                _hideToastCancellation?.Cancel();

                LastPurchasedItem.IsPurchased = false;
                UpdateItemCollections();
                ShowUndoToast = false;
                LastPurchasedItem = null;
            }
        }

        [RelayCommand]
        private void TogglePurchasedSection()
        {
            IsPurchasedSectionExpanded = !IsPurchasedSectionExpanded;
        }

        partial void OnShoppingListChanged(ShoppingList value)
        {
            if (value?.Items != null)
            {
                foreach (var item in value.Items)
                {
                    item.PropertyChanged += ShoppingItem_PropertyChanged;
                    item.ItemPurchased += ShoppingItem_OnItemPurchased;
                    item.ItemUnpurchased += ShoppingItem_OnItemUnpurchased;
                    item.CategoryChanged += ShoppingItem_OnCategoryChanged;
                }
                UpdateItemCollections();
            }
        }

        private async void ShoppingItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = sender as ShoppingItem;
            await _shoppingItemStore.UpdateShoppingItemAsync(item);

            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));
        }

        private async void ShoppingItem_OnItemPurchased(ShoppingItem item)
        {
            LastPurchasedItem = item;
            ShowUndoToast = true;

            UpdateItemCollections();

            // Cancel any existing hide timer
            _hideToastCancellation?.Cancel();
            _hideToastCancellation = new CancellationTokenSource();

            try
            {
                await Task.Delay(4000, _hideToastCancellation.Token);

                // Only hide if this is still the current item and token wasn't cancelled
                if (LastPurchasedItem == item && !_hideToastCancellation.Token.IsCancellationRequested)
                {
                    ShowUndoToast = false;
                }
            }
            catch (OperationCanceledException)
            {
                // Task was cancelled, which is expected behavior
            }
        }

        private void ShoppingItem_OnItemUnpurchased(ShoppingItem item)
        {
            UpdateItemCollections();
            if (LastPurchasedItem == item)
            {
                UndoLastPurchase();
            }
        }

        private void ShoppingItem_OnCategoryChanged(ShoppingItem item)
        {
            UpdateItemCollections();
        }

        private void UpdateItemCollections()
        {
            var activeItems = ShoppingList.Items
                .Where(item => !item.IsPurchased);

            var sortedActiveItems = _sortingItemsService.Sort(activeItems);

            ActiveItems = new ObservableCollection<ShoppingItem>(sortedActiveItems);

            var purchasedItems = ShoppingList.Items
                .Where(item => item.IsPurchased);

            PurchasedItems = new ObservableCollection<ShoppingItem>(purchasedItems);

            PurchasedSectionTitle = $"Purchased ({PurchasedItems.Count} items)";

            if(PurchasedItems.Count == 0) 
            {
                IsPurchasedSectionExpanded = false;
            }
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

                _hideToastCancellation?.Cancel();

                // Unsubscribe from events of items in the current ShoppingList
                if (ShoppingList != null)
                {
                    foreach (var item in ShoppingList.Items ?? Enumerable.Empty<ShoppingItem>())
                    {
                        item.PropertyChanged -= ShoppingItem_PropertyChanged;
                        item.ItemPurchased -= ShoppingItem_OnItemPurchased;
                        item.ItemUnpurchased -= ShoppingItem_OnItemUnpurchased;
                        item.CategoryChanged -= ShoppingItem_OnCategoryChanged;
                    }
                }
            }
        }
    }
}
