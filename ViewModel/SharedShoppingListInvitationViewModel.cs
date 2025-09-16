using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listly.Model;
using Listly.Store;
using Listly.View;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class SharedShoppingListInvitationViewModel : BaseViewModel
    {
        private readonly IShoppingListStore _store;
        private readonly string _shareId;
        private ShoppingList _sharedList;

        [ObservableProperty]
        string invitationMessage = "Loading invitation...";

        [ObservableProperty]
        bool isValidLink;

        public SharedShoppingListInvitationViewModel(IShoppingListStore store, string shareId)
        {
            _store = store;
            _shareId = shareId;

            _ = LoadSharedListAsync();
        }

        private async Task LoadSharedListAsync()
        {
            try
            {
                _sharedList = await _store.GetSharedListByShareIdAsync(_shareId);

                if (_sharedList?.IsShared == true && _sharedList.ShareExpiresAt > DateTime.UtcNow)
                {
                    InvitationMessage = $"List \"{_sharedList.Name}\" was shared with you!";
                    IsValidLink = true;
                }
                else
                {
                    InvitationMessage = "This invitation has expired or is no longer valid.";
                    IsValidLink = false;
                }
            }
            catch (Exception ex)
            {
                InvitationMessage = "Could not load the shared list.";
            }
        }

        [RelayCommand]
        private async Task OpenList()
        {
            if (_sharedList == null) return;

            await _store.AddCurrentUserAsCollaboratorOfShoppingList(_sharedList);

            await MopupService.Instance.PopAsync();

            await Shell.Current.GoToAsync(nameof(ShoppingListDetailsPage), true, new Dictionary<string, object>
            {
                { "ShoppingList", _sharedList}
            });
        }

        [RelayCommand]
        private async Task Discard()
        {
            await MopupService.Instance.PopAsync();
        }
    }
}
