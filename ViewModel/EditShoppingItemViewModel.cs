using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Mopups.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class EditShoppingItemViewModel : BaseViewModel
    {
        private readonly ShoppingItem _shoppingItem;

        public EditShoppingItemViewModel(ShoppingItem shoppingItem)
        {
            _shoppingItem = shoppingItem;
            Name = shoppingItem.Name;
            Quantity = shoppingItem.Quantity;
        }

        [ObservableProperty]
        string name;

        [ObservableProperty]
        int? quantity;

        [RelayCommand]
        void IncreaseQuantity() => Quantity = Quantity != null ? Quantity + 1 : 0;

        [RelayCommand]
        void DecreaseQuantity() => Quantity = Quantity != null ? Math.Max(0, Quantity.Value - 1) : 0;

        [RelayCommand]
        async Task Save()
        {
            if (!string.IsNullOrWhiteSpace(Name) && _shoppingItem.Name != Name ||
                _shoppingItem.Quantity != Quantity)
            {
                _shoppingItem.Name = Name;
                _shoppingItem.Quantity = Quantity;
                WeakReferenceMessenger.Default.Send(new ShoppingItemUpdatedMessage(_shoppingItem));
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
