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
    public partial class RenameListPopupViewModel : BaseViewModel
    {
        private readonly ShoppingList _list;
        private readonly string _originalName;

        [ObservableProperty]
        private string name;

        public bool HasChanges => !string.Equals(_originalName, Name?.Trim(), StringComparison.Ordinal);
        public bool CanSave => !string.IsNullOrWhiteSpace(Name?.Trim()) && HasChanges;

        public RenameListPopupViewModel(ShoppingList list)
        {
            _list = list;
            _originalName = list.Name ?? string.Empty;
            Name = _originalName;
            Title = "Rename List";
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        async Task Save()
        {
            var trimmedName = Name?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                await Shell.Current.DisplayAlert("Invalid Name", "Please enter a valid name for the list.", "OK");
                return;
            }

            if (!HasChanges)
            {
                await MopupService.Instance.PopAsync();
                return;
            }

            if (trimmedName.Length > 100)
            {
                await Shell.Current.DisplayAlert("Name Too Long", "List name cannot exceed 100 characters.", "OK");
                return;
            }

            _list.Name = trimmedName;
            WeakReferenceMessenger.Default.Send(new ShoppingListUpdatedMessage(_list));

            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        partial void OnNameChanged(string value)
        {
            OnPropertyChanged(nameof(HasChanges));
            OnPropertyChanged(nameof(CanSave));
            SaveCommand.NotifyCanExecuteChanged();
        }

    }
}
