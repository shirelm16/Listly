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


        [RelayCommand]
        private async Task Add()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return;

            var shoppingList = new ShoppingList
            {
                Name = Name,
                Id = Guid.NewGuid(),
                LastModified = DateTime.UtcNow
            };

            WeakReferenceMessenger.Default.Send(new ShoppingListCreatedMessage(shoppingList));
            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }
    }
}
