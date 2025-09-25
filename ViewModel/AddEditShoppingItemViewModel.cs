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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class AddEditShoppingItemViewModel : BaseViewModel
    {
        private readonly ShoppingItem _shoppingItem;

        private readonly IShoppingItemStore _shoppingItemStore;

        public Guid ShoppingListId { get; }

        public bool IsEditMode => _shoppingItem != null;

        public bool CanSave => !string.IsNullOrWhiteSpace(Name?.Trim()) && (!IsEditMode || HasChanges);

        public List<string> Categories { get; set; }

        [ObservableProperty]
        string name;

        [ObservableProperty]
        double? quantity;

        [ObservableProperty]
        string? unit;

        [ObservableProperty]
        string selectedCategory;


        public bool HasChanges
        {
            get
            {
                return !string.Equals(_shoppingItem?.Name, Name?.Trim(), StringComparison.Ordinal) ||
                       _shoppingItem?.Quantity != Quantity || _shoppingItem?.Unit != Unit || 
                       _shoppingItem?.Category?.Name.GetDisplayName() != SelectedCategory;
            }
        }

        public AddEditShoppingItemViewModel(IShoppingItemStore shoppingItemStore, ShoppingItem shoppingItem, Guid shoppingListId)
        {
            _shoppingItem = shoppingItem;
            _shoppingItemStore = shoppingItemStore;
            ShoppingListId = shoppingListId;
            Name = shoppingItem?.Name;
            Quantity = shoppingItem?.Quantity;
            Unit = shoppingItem?.Unit;
            Categories = Enum.GetValues<Category>()
                .Select(e => e.GetDisplayWithIcon())
                .ToList();
            SelectedCategory = shoppingItem?.Category?.Name == null ? null : shoppingItem.Category.Name.GetDisplayWithIcon();
            Title = IsEditMode ? "Edit Item" : "Add Item";
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

                if (trimmedName.Length > 200)
                {
                    await Shell.Current.DisplayAlert("Name Too Long", "Item name cannot exceed 200 characters.", "OK");
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
            var hasCategoryChanged = _shoppingItem.Category.Name.GetDisplayName() != SelectedCategory;

            if (hasNameChanged || hasQuantityChanged || hasCategoryChanged)
            {
                _shoppingItem.Name = trimmedName;
                _shoppingItem.Quantity = Quantity;
                _shoppingItem.Unit = Unit;

                var category = CategoryHelper.FromDisplayNameAndIcon(SelectedCategory);
                _shoppingItem.Category = new ItemCategory(category);

                await _shoppingItemStore.UpdateShoppingItemAsync(_shoppingItem);
                WeakReferenceMessenger.Default.Send(new ShoppingItemUpdatedMessage(_shoppingItem));
            }
        }

        private async Task CreateNewItem(string trimmedName)
        {
            var category = SelectedCategory != null ? CategoryHelper.FromDisplayNameAndIcon(SelectedCategory) : Category.Other;
            var shoppingItem = new ShoppingItem(ShoppingListId, trimmedName, Quantity, Unit, new ItemCategory(category));

            await _shoppingItemStore.CreateShoppingItemAsync(shoppingItem);
            WeakReferenceMessenger.Default.Send(new ShoppingItemCreatedMessage(shoppingItem));
        }

        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();
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

        partial void OnSelectedCategoryChanged(string value)
        {
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(HasChanges));
            SaveCommand.NotifyCanExecuteChanged();
        }
    }
}
