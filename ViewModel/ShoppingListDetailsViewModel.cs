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
        private BucketSortService<ShoppingItem, Category> _activeBuckets;
        private List<ShoppingItem> _purchasedItems;

        public ShoppingListDetailsViewModel(IShoppingItemStore shoppingItemStore, IShoppingItemsSortingService sortingItemsService)
        {
            _shoppingItemStore = shoppingItemStore;

            var categoryOrder = Enum.GetValues<Category>().OrderBy(c => c);
            _activeBuckets = new BucketSortService<ShoppingItem, Category>(item => item?.Category == null ? Category.Other : item.Category.Name, categoryOrder);
            _purchasedItems = shoppingList == null ? [] :
                shoppingList.Items.Where(item => item.IsPurchased).ToList();
            
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
                   
                    _activeBuckets.AddItem(item);
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
        private CancellationTokenSource? _updateCancellation;

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

            if (shoppingItem.IsPurchased)
            {
                _purchasedItems.Remove(shoppingItem);
            }
            else
            {
                _activeBuckets.RemoveItem(shoppingItem);
            }
            
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
                HideUndoToast();
                LastPurchasedItem.IsPurchased = false;
                LastPurchasedItem = null;
            }
        }

        private void HideUndoToast()
        {
            _hideToastCancellation?.Cancel();
            ShowUndoToast = false;
        }

        [RelayCommand]
        private void TogglePurchasedSection()
        {
            IsPurchasedSectionExpanded = !IsPurchasedSectionExpanded;
        }

        partial void OnShoppingListChanged(ShoppingList value)
        {
            _activeBuckets.Clear();
            _purchasedItems = new List<ShoppingItem>();

            if (value?.Items != null)
            {
                foreach (var item in value.Items)
                {
                    item.PropertyChanged += ShoppingItem_PropertyChanged;
                    item.ItemPurchased += ShoppingItem_OnItemPurchased;
                    item.ItemUnpurchased += ShoppingItem_OnItemUnpurchased;
                    item.CategoryChanged += ShoppingItem_OnCategoryChanged;
                    
                    if(item.IsPurchased)
                    {
                        _purchasedItems.Add(item);
                    }
                    else
                    {
                        _activeBuckets.AddItem(item);
                    }
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

            _activeBuckets.RemoveItem(item);
            _purchasedItems.Add(item);
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
            _activeBuckets.AddItem(item);
            _purchasedItems.Remove(item);

            UpdateItemCollections();
            if (LastPurchasedItem == item)
            {
                HideUndoToast();
            }
        }

        private void ShoppingItem_OnCategoryChanged(ShoppingItem item)
        {
            _activeBuckets.UpdateItem(item);
            UpdateItemCollections();
        }

        private void UpdateItemCollections()
        {
            ActiveItems = new ObservableCollection<ShoppingItem>(_activeBuckets.GetSortedItems());
            PurchasedItems = new ObservableCollection<ShoppingItem>(_purchasedItems);

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
