using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Services;
using Listly.Store;
using Mopups.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class AddEditShoppingItemViewModel : BaseViewModel
    {
        private readonly ShoppingItem _shoppingItem;

        private readonly IShoppingItemStore _shoppingItemStore;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUsersStore _usersStore;

        private readonly ICategorySuggestionService _categorySuggestionService;
        private CancellationTokenSource? _suggestionCts;

        private bool _isInitializing;
        private bool _isSettingCategoryProgrammatically;
        private bool _wasCategoryManuallyChanged;

        public Guid ShoppingListId { get; }

        public bool IsEditMode => _shoppingItem != null;

        public bool CanSave => !string.IsNullOrWhiteSpace(Name?.Trim()) && (!IsEditMode || HasChanges);

        public List<string> Categories { get; set; }
        public List<Priority> Priorities { get; set; }

        public bool ShowSuggestButton => !IsSuggestingCategory && !string.IsNullOrWhiteSpace(Name);

        [ObservableProperty]
        string name;

        [ObservableProperty]
        double? quantity;

        [ObservableProperty]
        string? unit;

        [ObservableProperty]
        string? selectedCategory;

        [ObservableProperty]
        Priority? itemPriority;

        [ObservableProperty]
        bool? hasPriority;

        [ObservableProperty]
        bool isSuggestingCategory;


        public bool HasChanges
        {
            get
            {
                return !string.Equals(_shoppingItem?.Name, Name?.Trim(), StringComparison.Ordinal) ||
                       _shoppingItem?.Quantity != Quantity || _shoppingItem?.Unit != Unit || 
                       _shoppingItem?.Category?.Name.GetDisplayWithIcon() != SelectedCategory ||
                       _shoppingItem?.Priority != ItemPriority || _shoppingItem?.HasPriority != HasPriority;
            }
        }

        public AddEditShoppingItemViewModel(IShoppingItemStore shoppingItemStore, IUsersStore usersStore,
            ICurrentUserService currentUserService, ICategorySuggestionService categorySuggestionService, 
            ShoppingItem shoppingItem, Guid shoppingListId)
        {
            _isInitializing = true;
            _shoppingItem = shoppingItem;
            _shoppingItemStore = shoppingItemStore;
            _usersStore = usersStore;
            _currentUserService = currentUserService;
            _categorySuggestionService = categorySuggestionService;
            ShoppingListId = shoppingListId;
            Name = shoppingItem?.Name;
            Quantity = shoppingItem?.Quantity;
            Unit = shoppingItem?.Unit;
            Categories = Enum.GetValues<Category>()
                .Select(e => e.GetDisplayWithIcon())
                .ToList();
            Priorities = Enum.GetValues<Priority>().ToList();
            HasPriority = shoppingItem?.HasPriority ?? false;
            ItemPriority = shoppingItem?.Priority;
            SelectedCategory = shoppingItem?.Category.Name.GetDisplayWithIcon();
            Title = IsEditMode ? "Edit Item" : "Add Item";
            _isInitializing = false;
        }

        [RelayCommand]
        async Task SuggestCategory()
        {
            await SuggestCategoryAsync(Name);
        }

        [RelayCommand]
        void IncreaseQuantity()
        {
            Quantity = (Quantity ?? 0) + 1;
            if (Quantity <= 0) Quantity = 1;
        }

        [RelayCommand]
        void DecreaseQuantity()
        {
            if (Quantity.HasValue)
            {
                Quantity = Math.Max(0, Quantity.Value - 1);
                if (Quantity == 0) Quantity = null;
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        async Task Save()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                var trimmedName = Name?.Trim();

                if (string.IsNullOrWhiteSpace(trimmedName))
                {
                    await Shell.Current.DisplayAlert("Invalid Name", "Please enter a valid name for the item.", "OK");
                    return;
                }

                if (trimmedName.Length > 50)
                {
                    await Shell.Current.DisplayAlert("Name Too Long", "Item name cannot exceed 50 characters.", "OK");
                    return;
                }

                if (Quantity.HasValue && Quantity.Value <= 0)
                {
                    await Shell.Current.DisplayAlert("Invalid Quantity", "Quantity must be greater than 0.", "OK");
                    return;
                }

                if (IsEditMode)
                {
                    await UpdateExistingItem(trimmedName);
                }
                else
                {
                    await CreateNewItem(trimmedName);
                }

                await MopupService.Instance.PopAsync();
            }
        }

        [RelayCommand]
        async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        private async Task UpdateExistingItem(string trimmedName)
        {
            if (_shoppingItem == null) 
                return;

            var hasNameChanged = !string.Equals(_shoppingItem.Name, trimmedName, StringComparison.Ordinal);
            var hasQuantityChanged = _shoppingItem.Quantity != Quantity || _shoppingItem.Unit != Unit;
            var hasCategoryChanged = _shoppingItem.Category.Name.GetDisplayWithIcon() != SelectedCategory;
            var hasPriorityChanged = _shoppingItem.HasPriority != HasPriority || _shoppingItem.Priority != ItemPriority;

            if (hasNameChanged || hasQuantityChanged || hasCategoryChanged || hasPriorityChanged)
            {
                _shoppingItem.Name = trimmedName;
                _shoppingItem.Quantity = Quantity;
                _shoppingItem.Unit = Unit;
                _shoppingItem.HasPriority = HasPriority ?? false;
                _shoppingItem.Priority = HasPriority == true ? ItemPriority.Value : Priority.Medium;

                var category = CategoryHelper.FromDisplayNameAndIcon(SelectedCategory);
                _shoppingItem.Category = new ItemCategory(category);

                await SaveCategoryOverride();
                await _shoppingItemStore.UpdateShoppingItemAsync(_shoppingItem);
                WeakReferenceMessenger.Default.Send(new ShoppingItemUpdatedMessage(_shoppingItem));
            }
        }

        private async Task CreateNewItem(string trimmedName)
        {
            var category = SelectedCategory != null ? CategoryHelper.FromDisplayNameAndIcon(SelectedCategory) : Category.Other;
            var priority = HasPriority == true ? ItemPriority.Value : Priority.Medium;
            var shoppingItem = new ShoppingItem(ShoppingListId, trimmedName, Quantity, Unit, new ItemCategory(category), priority, HasPriority ?? false);

            await SaveCategoryOverride();
            await _shoppingItemStore.CreateShoppingItemAsync(shoppingItem);
            WeakReferenceMessenger.Default.Send(new ShoppingItemCreatedMessage(shoppingItem));
        }

        private async Task SaveCategoryOverride()
        {
            if (!_wasCategoryManuallyChanged)
                return;

            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(SelectedCategory))
                return;

            var userId = _currentUserService.UserId;
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var category = CategoryHelper.FromDisplayNameAndIcon(SelectedCategory);

            await _usersStore.SaveUserCategoryOverride(
                userId,
                category.ToString(),
                Name);
        }

        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();

            if (_isInitializing)
                return;

            OnPropertyChanged(nameof(ShowSuggestButton));
        }

        private async Task SuggestCategoryAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
                return;

            _suggestionCts?.Cancel();
            _suggestionCts = new CancellationTokenSource();

            IsSuggestingCategory = true;
            OnPropertyChanged(nameof(ShowSuggestButton));

            try
            {
                var category = await _categorySuggestionService.SuggestCategoryAsync(name, _currentUserService.UserId ?? Guid.Empty.ToString());

                if (category != null)
                {
                    _isSettingCategoryProgrammatically = true;
                    SelectedCategory = category.Value.GetDisplayWithIcon();
                    _isSettingCategoryProgrammatically = false;
                }
            }
            catch (OperationCanceledException)
            {
                // expected when popup closes or user types again
            }
            finally
            {
                IsSuggestingCategory = false;
            }
        }

        partial void OnQuantityChanged(double? value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();
        }

        partial void OnUnitChanged(string? value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();
        }

        partial void OnSelectedCategoryChanged(string? value)
        {
            if (!_isInitializing && !_isSettingCategoryProgrammatically)
            {
                _wasCategoryManuallyChanged = true;
            }
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();
        }

        partial void OnItemPriorityChanged(Priority? value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();
        }

        partial void OnHasPriorityChanged(bool? value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();

            if (value == false)
            {
                ItemPriority = Priority.Medium;
            }
        }
    }
}
