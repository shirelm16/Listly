using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class AddShoppingListPopupViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string ownerId;

        public bool CanAdd => !string.IsNullOrWhiteSpace(Name?.Trim());

        public AddShoppingListPopupViewModel()
        {
            Title = "Add New List";
        }


        [RelayCommand(CanExecute = nameof(CanAdd))]
        private async Task Add()
        {
            var trimmedName = Name?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                await Shell.Current.DisplayAlert("Invalid Name", "Please enter a valid name for the list.", "OK");
                return;
            }

            // Validate name length (optional business rule)
            if (trimmedName.Length > 100)
            {
                await Shell.Current.DisplayAlert("Name Too Long", "List name cannot exceed 100 characters.", "OK");
                return;
            }

            var shoppingList = new ShoppingList(trimmedName, OwnerId);

            WeakReferenceMessenger.Default.Send(new ShoppingListCreatedMessage(shoppingList));

            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(CanAdd));
            AddCommand.NotifyCanExecuteChanged();
        }
    }
}
