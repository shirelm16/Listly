using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Store;
using Listly.View;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class ShoppingListsViewModel : BaseViewModel
    {
        private readonly ShoppingListStore _shoppingListStore;

        [ObservableProperty]
        ObservableCollection<ShoppingList> _shoppingLists = new();

        public ShoppingListsViewModel(ShoppingListStore shoppingListStore)
        {
            Title = "My Lists";
            _shoppingListStore = shoppingListStore;

            WeakReferenceMessenger.Default.Register<ShoppingListUpdatedMessage>(this, async (r, m) =>
            {
                var list = m.Value;
                list.LastModified = DateTime.UtcNow;
                await _shoppingListStore.UpdateShoppingList(list);
            });

            WeakReferenceMessenger.Default.Register<ShoppingListCreatedMessage>(this, async (r, m) =>
            {
                var list = m.Value;
                await _shoppingListStore.AddShoppingList(list);
                ShoppingLists.Add(list);
            });
        }

        [RelayCommand]
        async Task DeleteList(ShoppingList shoppingList)
        {
            if (shoppingList == null)
                return;

            await _shoppingListStore.RemoveShoppingList(shoppingList.Id);
            ShoppingLists.Remove(shoppingList);
        }

        [RelayCommand]
        async Task RenameList(ShoppingList shoppingList)
        {
            if (shoppingList == null)
                return;
            var popup = new RenameListPopup(shoppingList);
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        async Task AddList()
        {
            var popup = new AddShoppingListPopup();
            await MopupService.Instance.PushAsync(popup);
        }

        [RelayCommand]
        async Task GoToDetails(ShoppingList shoppingList)
        {
            if (shoppingList == null)
                return;

            await Shell.Current.GoToAsync(nameof(ShoppingListDetailsPage), true, new Dictionary<string, object>
            {
                { "ShoppingList", shoppingList}
            });
        }

        [RelayCommand]
        async Task GetShoppingListsAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var shoppingLists = await _shoppingListStore.GetShoppingLists();

                if (ShoppingLists.Count > 0)
                    ShoppingLists.Clear();

                foreach (var shoppingList in shoppingLists)
                    ShoppingLists.Add(shoppingList);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error!", $"Unable to get shopping lists: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
