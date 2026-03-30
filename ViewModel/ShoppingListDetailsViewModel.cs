using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Services;
using Listly.Store;
using Listly.View;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Listly.ViewModel
{
    [QueryProperty(nameof(ShoppingList), "ShoppingList")]
    public partial class ShoppingListDetailsViewModel : BaseViewModel, IDisposable
    {
        private readonly IShoppingItemStore _shoppingItemStore;
        private readonly IUsersStore _usersStore;
        private readonly ICurrentUserService _currentUserService;
        private IBucketSortService<ShoppingItem> _activeBuckets;
        private readonly ICategorySuggestionService _categorySuggestionService;
        private readonly IImportFromRecipeService _importFromRecipeService;
        private List<ShoppingItem> _purchasedItems;
        private IDisposable? _itemsListener;
        private bool _isApplyingRemoteUpdate = false;

        private ShoppingItemsGroup? _activeGroup;
        private ShoppingItemsGroup? _purchasedGroup;

        public ShoppingListDetailsViewModel(IShoppingItemStore shoppingItemStore, IUsersStore usersStore, 
            ICurrentUserService currentUserService, ICategorySuggestionService categorySuggestionService, 
            IImportFromRecipeService importFromRecipeService)
        {
            _shoppingItemStore = shoppingItemStore;
            _usersStore = usersStore;
            _currentUserService = currentUserService;
            _categorySuggestionService = categorySuggestionService;
            _purchasedItems = shoppingList == null ? [] :
                shoppingList.Items.Where(item => item.IsPurchased).ToList();
            _importFromRecipeService = importFromRecipeService;

            WeakReferenceMessenger.Default.Register<AddRecipeItemsToListMessage>(this, async (r, m) =>
            {
                if (m.Value.ListId != ShoppingList.Id)
                    return;

                var newItems = m.Value.Items.Select(suggestion =>
                {
                    var category = suggestion.SuggestedCategory != null
                        ? CategoryHelper.FromDisplayNameAndIcon(suggestion.SuggestedCategory)
                        : Category.Other;

                    return new ShoppingItem(
                        m.Value.ListId,
                        suggestion.Name,
                        suggestion.Quantity,
                        suggestion.Unit,
                        new ItemCategory(category));
                }).ToList();

                await _shoppingItemStore.CreateShoppingItemsBatchAsync(newItems);
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
        private ObservableCollection<ShoppingItemsGroup> groupedItems = new();

        private bool isPurchasedSectionExpanded = false;

        [ObservableProperty]
        private bool isSelectionMode = false;

        [ObservableProperty]
        private bool isSearchVisible = false;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private HashSet<Guid> selectedItemIds = new();

        [ObservableProperty]
        private int selectedCount = 0;

        public string SelectionText => $"{SelectedCount} selected";

        public bool IsNormalMode => !IsSelectionMode;

        [RelayCommand]
        async Task ShowImportFromRecipePopup()
        {
            await MopupService.Instance.PopAsync();
            var popup = new ImportFromRecipePopup(_importFromRecipeService, ShoppingList.Id);
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        async Task Back()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task ShowMoreMenu()
        {
            var popup = new ShoppingListDetailsMenuPopup(this);
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        private async Task ShowSearch()
        {
            IsSearchVisible = true;
            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private void CloseSearch()
        {
            SearchText = string.Empty;
            IsSearchVisible = false;
            RestoreFullGroupedItems();
        }

        [RelayCommand]
        private async Task DeletePurchasedItems()
        {
            if (_purchasedItems.Count == 0)
            {
                await MopupService.Instance.PopAsync();
                await Shell.Current.DisplayAlert("Delete purchased items", "No items to delete", "Ok");
                return;
            }

            var confirmed = await Shell.Current.DisplayAlert(
                "Delete purchased items",
                $"Are you sure you want to delete all purchased items ({_purchasedItems.Count})?",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            foreach (var item in _purchasedItems.ToList())
            {
                ShoppingList.Items.Remove(item);
                await _shoppingItemStore.DeleteShoppingItemAsync(item.ShoppingListId, item.Id);
            }

            _purchasedItems.Clear();

            UpdateItemCollections();

            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));

            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task ShowSortOptions()
        {
            await MopupService.Instance.PopAsync();
            var popupVm = new SortOptionsPopupViewModel(this, sortType =>
            {
                ChangeSortType(sortType);
            });
            var popup = new SortOptionsPopup(popupVm);
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        async Task EditItem(ShoppingItem shoppingItem)
        {
            if (shoppingItem == null)
                return;

            var popup = new AddEditShoppingItemPopup(_shoppingItemStore, _usersStore, _currentUserService, _categorySuggestionService, ShoppingList.Id, shoppingItem);
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
            var popup = new AddEditShoppingItemPopup(_shoppingItemStore, _usersStore, _currentUserService, _categorySuggestionService, ShoppingList.Id);
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
        private void TogglePurchasedGroup(ShoppingItemsGroup group)
        {
            if (group.IsExpandable)
            {
                group.SetExpanded(!group.IsExpanded);
                if (group.IsExpanded) 
                {
                    group.RefreshItems(_purchasedItems);
                }
                else
                {
                    group.RefreshItems([]);
                }
                isPurchasedSectionExpanded = group.IsExpanded;
            }
        }

        [RelayCommand]
        private void ItemLongPress(ShoppingItem item)
        {
            if (item == null) return;

            IsSelectionMode = true;
            SelectedItemIds = new HashSet<Guid>(SelectedItemIds) { item.Id };
            SelectedCount = SelectedItemIds.Count;
        }

        [RelayCommand]
        private void ItemTapped(ShoppingItem item)
        {
            if (item == null) return;

            if (IsSelectionMode)
            {
                var newSelection = new HashSet<Guid>(SelectedItemIds);

                if (newSelection.Contains(item.Id))
                {
                    newSelection.Remove(item.Id);
                }
                else
                {
                    newSelection.Add(item.Id);
                }

                SelectedItemIds = newSelection;
                SelectedCount = SelectedItemIds.Count;

                // Exit selection mode if no items selected
                if (SelectedCount == 0)
                {
                    IsSelectionMode = false;
                }
            }
            else
            {
                EditItemCommand.Execute(item);
            }
        }

        [RelayCommand]
        private async Task DeleteSelected()
        {
            if (SelectedCount == 0) return;

            var confirmed = await Shell.Current.DisplayAlert(
                "Delete Items",
                $"Are you sure you want to delete {SelectedCount} item(s)?",
                "Delete",
                "Cancel");

            if (!confirmed) return;

            var itemsToDelete = ShoppingList.Items
                .Where(item => SelectedItemIds.Contains(item.Id))
                .ToList();

            foreach (var item in itemsToDelete)
            {
                if (item.IsPurchased)
                {
                    _purchasedItems.Remove(item);
                }
                else
                {
                    _activeBuckets.RemoveItem(item);
                }

                ShoppingList.Items.Remove(item);
                await _shoppingItemStore.DeleteShoppingItemAsync(item.ShoppingListId, item.Id);
            }

            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));

            // Exit selection mode
            IsSelectionMode = false;
            UpdateItemCollections();
        }

        [RelayCommand]
        private void CancelSelection()
        {
            IsSelectionMode = false;
        }

        partial void OnShoppingListChanged(ShoppingList value)
        {
            SetActiveItemsBySortType(shoppingList?.SortType);
            _purchasedItems = new List<ShoppingItem>();

            _itemsListener?.Dispose();
            _itemsListener = null;

            if (value?.Items != null)
            {
                foreach (var item in value.Items)
                {
                    SubscribeToItemEvents(item);

                    if (item.IsPurchased)
                    {
                        _purchasedItems.Add(item);
                    }
                    else
                    {
                        _activeBuckets.AddItem(item);
                    }
                }
                InitializeGroups();

                _itemsListener = _shoppingItemStore.ListenToItems(value.Id, OnRemoteItemsChanged);
            }
        }

        partial void OnIsSelectionModeChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNormalMode));

            // Clear selections when exiting selection mode
            if (!value)
            {
                SelectedItemIds.Clear();
                SelectedCount = 0;
                OnPropertyChanged(nameof(SelectionText));
                OnPropertyChanged(nameof(SelectedItemIds));
            }
        }

        partial void OnSelectedCountChanged(int value)
        {
            OnPropertyChanged(nameof(SelectionText));
        }

        private async void ShoppingItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isApplyingRemoteUpdate) return;

            var item = sender as ShoppingItem;
            await _shoppingItemStore.UpdateShoppingItemAsync(item, e.PropertyName);

            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));
        }

        private async void ShoppingItem_OnItemPurchased(ShoppingItem item)
        {
            LastPurchasedItem = item;
            ShowUndoToast = true;

            await Task.Delay(700);

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
            if (ShoppingList.SortType == SortType.Category)
            {
                _activeBuckets.UpdateItem(item);
                UpdateItemCollections();
            }
        }

        private void ShoppingItem_OnPriorityChanged(ShoppingItem item)
        {
            if (ShoppingList.SortType == SortType.Priority)
            {
                _activeBuckets.UpdateItem(item);
                UpdateItemCollections();
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplySearch();
        }

        private void ApplySearch()
        {
            var normalizedText = Normalize(SearchText);

            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                RestoreFullGroupedItems();
                return;
            }

            var activeItems = _activeBuckets.GetSortedItems();

            var allItems = activeItems.Concat(_purchasedItems);

            var matchedItems = allItems
                .Where(item => Normalize(item.Name).Contains(normalizedText))
                .ToList();

            RebuildFilteredDisplay(matchedItems);
        }

        private void RestoreFullGroupedItems()
        {
            _activeGroup?.RefreshItems(_activeBuckets.GetSortedItems().ToList());

            if (_purchasedGroup != null)
            {
                _purchasedGroup.SetExpanded(isPurchasedSectionExpanded);
                _purchasedGroup.Title = $"Purchased ({_purchasedItems.Count} items)";

                if (isPurchasedSectionExpanded)
                {
                    _purchasedGroup.RefreshItems(_purchasedItems);
                }
                else
                {
                    _purchasedGroup.RefreshItems([]);
                }
            }
        }

        private void InitializeGroups()
        {
            GroupedItems = new ObservableCollection<ShoppingItemsGroup>();

            _activeGroup = new ShoppingItemsGroup("", _activeBuckets.GetSortedItems());
            GroupedItems.Add(_activeGroup);

            _purchasedGroup = new ShoppingItemsGroup($"Purchased ({_purchasedItems.Count} items)", 
                _purchasedItems, isExpanded: isPurchasedSectionExpanded, isExpandable: true);
            GroupedItems.Add(_purchasedGroup);
        }

        private void UpdateItemCollections()
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                ApplySearch();
                return;
            }

            if (_purchasedItems.Count == 0)
            {
                isPurchasedSectionExpanded = false;
            }

            if (_activeGroup != null)
            {
                var activeItems = _activeBuckets.GetSortedItems().ToList();

                if (!_activeGroup.SequenceEqual(activeItems))
                {
                    _activeGroup.RefreshItems(activeItems);
                }
            }

            if (_purchasedGroup != null)
            {
                _purchasedGroup.Title = $"Purchased ({_purchasedItems.Count} items)";
                _purchasedGroup.SetExpanded(isPurchasedSectionExpanded);
                //update purchasedGroup if expanded or if purchased items became empty
                if (_purchasedGroup.IsExpanded || _purchasedItems.Count == 0)
                {
                    if (!_purchasedGroup.SequenceEqual(_purchasedItems))
                    {
                        _purchasedGroup.RefreshItems(_purchasedItems);
                    }
                }
            }
        }

        private void ChangeSortType(SortType sortType)
        {
            if ((ShoppingList.SortType == null && sortType != SortType.Category) || sortType != ShoppingList.SortType)
            {
                SetActiveItemsBySortType(sortType);
                foreach (var item in ShoppingList.Items)
                {
                    if (!item.IsPurchased)
                        _activeBuckets.AddItem(item);
                }

                UpdateItemCollections();

                ShoppingList.SortType = sortType;
                WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(ShoppingList));
            }
        }

        private void SetActiveItemsBySortType(SortType? sortType)
        {
            if (sortType == null || sortType == SortType.Category)
            {
                var categoryOrder = Enum.GetValues<Category>().OrderBy(c => c);
                _activeBuckets = new BucketSortService<ShoppingItem, Category>(item => item?.Category == null ? Category.Other : item.Category.Name, categoryOrder);
            }
            else if (sortType == SortType.Priority)
            {
                var priorityOrder = new[] { Priority.High, Priority.Medium, Priority.Low };
                _activeBuckets = new BucketSortService<ShoppingItem, Priority>(item => item?.Priority == null ? Priority.Medium : item.Priority, priorityOrder);
            }
        }

        private void RebuildFilteredDisplay(List<ShoppingItem> matchedItems)
        {
            var activeItems = matchedItems.Where(i => !i.IsPurchased).ToList();
            var purchasedItems = matchedItems.Where(i => i.IsPurchased).ToList();

            _activeGroup?.RefreshItems(activeItems);

            if (_purchasedGroup != null) 
            {
                _purchasedGroup.SetExpanded(true);
                _purchasedGroup.RefreshItems(purchasedItems);
                _purchasedGroup.Title = $"Purchased ({purchasedItems.Count} items)";
            }
        }

        private string Normalize(string text)
        {
            return text?.Trim().ToLowerInvariant() ?? string.Empty;
        }

        private void SubscribeToItemEvents(ShoppingItem item)
        {
            item.PropertyChanged += ShoppingItem_PropertyChanged;
            item.ItemPurchased += ShoppingItem_OnItemPurchased;
            item.ItemUnpurchased += ShoppingItem_OnItemUnpurchased;
            item.CategoryChanged += ShoppingItem_OnCategoryChanged;
            item.PriorityChanged += ShoppingItem_OnPriorityChanged;
        }

        private void UnsubscribeFromItemEvents(ShoppingItem item)
        {
            item.PropertyChanged -= ShoppingItem_PropertyChanged;
            item.ItemPurchased -= ShoppingItem_OnItemPurchased;
            item.ItemUnpurchased -= ShoppingItem_OnItemUnpurchased;
            item.CategoryChanged -= ShoppingItem_OnCategoryChanged;
            item.PriorityChanged -= ShoppingItem_OnPriorityChanged;
        }

        private void OnRemoteItemsChanged(IEnumerable<ShoppingItem> remoteItems)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ShoppingList == null) return;

                _isApplyingRemoteUpdate = true;
                try
                {
                    var remoteById = remoteItems.ToDictionary(i => i.Id);
                    var localById = ShoppingList.Items.ToDictionary(i => i.Id);
                    bool needsUpdate = false;

                    // Items deleted remotely
                    foreach (var id in localById.Keys.Except(remoteById.Keys).ToList())
                    {
                        var item = localById[id];
                        UnsubscribeFromItemEvents(item);
                        if (item.IsPurchased)
                            _purchasedItems.Remove(item);
                        else
                            _activeBuckets.RemoveItem(item);
                        ShoppingList.Items.Remove(item);
                        needsUpdate = true;
                    }

                    // Items added remotely
                    foreach (var id in remoteById.Keys.Except(localById.Keys).ToList())
                    {
                        var item = remoteById[id];
                        SubscribeToItemEvents(item);
                        ShoppingList.Items.Add(item);
                        if (item.IsPurchased)
                            _purchasedItems.Add(item);
                        else
                            _activeBuckets.AddItem(item);
                        needsUpdate = true;
                    }

                    // Items updated remotely
                    foreach (var remoteItem in remoteById.Values.Where(i => localById.ContainsKey(i.Id)))
                    {
                        var localItem = localById[remoteItem.Id];
                        if (ShoppingItem.AreItemsEqual(localItem, remoteItem)) continue;

                        var wasPurchased = localItem.IsPurchased;

                        // Suppress event handlers while applying remote values
                        UnsubscribeFromItemEvents(localItem);

                        localItem.UpdateFrom(remoteItem);

                        SubscribeToItemEvents(localItem);

                        if (wasPurchased != remoteItem.IsPurchased)
                        {
                            if (remoteItem.IsPurchased)
                            {
                                _activeBuckets.RemoveItem(localItem);
                                _purchasedItems.Add(localItem);
                            }
                            else
                            {
                                _purchasedItems.Remove(localItem);
                                _activeBuckets.AddItem(localItem);
                            }
                        }
                        else if (!remoteItem.IsPurchased)
                        {
                            _activeBuckets.UpdateItem(localItem);
                        }

                        needsUpdate = true;
                    }

                    if (needsUpdate)
                        UpdateItemCollections();
                }
                finally
                {
                    _isApplyingRemoteUpdate = false;
                }
            });
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
                _itemsListener?.Dispose();

                // Unsubscribe from events of items in the current ShoppingList
                if (ShoppingList != null)
                {
                    foreach (var item in ShoppingList.Items ?? Enumerable.Empty<ShoppingItem>())
                    {
                        UnsubscribeFromItemEvents(item);
                    }
                }
            }
        }
    }
}
