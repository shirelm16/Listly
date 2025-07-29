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

        [ObservableProperty]
        private string name;

        public RenameListPopupViewModel(ShoppingList list)
        {
            _list = list;
            Name = list.Name;
        }

        [RelayCommand]
        async Task Save()
        {
            if (!string.IsNullOrWhiteSpace(Name) && _list.Name != Name)
            {
                _list.Name = Name;
                _list.LastModified = DateTime.UtcNow;
                WeakReferenceMessenger.Default.Send(new ShoppingListRenamedMessage(_list));
            }

            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

    }
}
