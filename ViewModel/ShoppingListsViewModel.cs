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
        public ObservableCollection<ShoppingList> ShoppingLists { get; } = new();

        public ShoppingListsViewModel(ShoppingListStore shoppingListStore)
        {
            Title = "My Lists";
            _shoppingListStore = shoppingListStore;

            WeakReferenceMessenger.Default.Register<ShoppingListRenamedMessage>(this, async (r, m) =>
            {
                var list = m.Value;
                await _shoppingListStore.RenameShoppingList(list.Id, list.Name);
            });
        }

        [RelayCommand]
        async Task OpenMenuPopup(ShoppingList selectedList)
        {
            await MopupService.Instance.PushAsync(new ListMenuPopup(selectedList, this));
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
        async Task AddShoppingListAsync()
        {
            var result = await Shell.Current.DisplayPromptAsync(
                "New Shopping List",
                "Enter list name:",
                "Create", "Cancel");

            if (!string.IsNullOrWhiteSpace(result))
            {
                var shoppingList = new ShoppingList
                {
                    Name = result,
                    Id = Guid.NewGuid()
                };
                await _shoppingListStore.AddShoppingList(shoppingList);
                ShoppingLists.Add(shoppingList);
            }
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

        public void Refresh()
        {
            OnPropertyChanged(nameof(ShoppingLists));
        }
    }
}
