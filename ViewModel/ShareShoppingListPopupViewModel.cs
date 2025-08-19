using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listly.Model;
using Listly.Store;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Listly.ViewModel
{
    public partial class ShareShoppingListpPopupViewModel : BaseViewModel
    {
        private readonly IShoppingListStore _store;
        private readonly ShoppingList _list;

        [ObservableProperty]
        private string shareLink = "Generating link...";

        public ShareShoppingListpPopupViewModel(IShoppingListStore store, ShoppingList list)
        {
            _store = store;
            _list = list;

            _ = GenerateShareLinkAsync();
        }

        [RelayCommand]
        private async Task CopyLink()
        {
            if (!string.IsNullOrEmpty(ShareLink) && ShareLink != "Generating link...")
            {
                await Clipboard.SetTextAsync(ShareLink);
            }
        }

        [RelayCommand]
        private async Task Share()
        {
            if (!string.IsNullOrEmpty(ShareLink) && ShareLink != "Generating link...")
            {
                await Microsoft.Maui.ApplicationModel.DataTransfer.Share.RequestAsync(new ShareTextRequest
                {
                    Text = $"Check out my shopping list: {ShareLink}",
                    Title = "Shopping List"
                });
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        private async Task GenerateShareLinkAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_list.ShareId))
                {
                    _list.ShareId = Guid.NewGuid().ToString("N")[..8];
                    _list.ShareExpiresAt = DateTime.UtcNow.AddDays(30);
                    await _store.UpdateShoppingListAsync(_list);
                }

                ShareLink = $"https://{Consts.AppHost}/shared/{_list.ShareId}";
            }
            catch (Exception ex)
            {
                ShareLink = "Error generating link";
            }
        }
    }
}
