using Listly.Service;
using Listly.Store;
using Listly.View;
using Listly.ViewModel;
using Mopups.Services;
using Plugin.Firebase.CloudMessaging;
using System.Diagnostics;

namespace Listly
{
    public partial class App : Application
    {
        private readonly IShoppingListStore _shoppingListsStore;
        private readonly IUsersStore _usersStore;

        public App(IAuthService authService, IShoppingListStore shoppingListsStore, IUsersStore usersStore)
        {
            InitializeComponent();
            _shoppingListsStore = shoppingListsStore;
            _usersStore = usersStore;
            MainPage = new AppShell();

            CrossFirebaseCloudMessaging.Current.TokenChanged += async (s, newToken) =>
            {
                var userId = await authService.GetCurrentUserIdAsync();
                if (userId != null)
                {
                    await _usersStore.UpdateDeviceToken(userId, newToken.Token);
                }
            };
        }

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            if (uri.AbsolutePath.StartsWith("/shared/"))
            {
                var shareId = uri.AbsolutePath.Replace("/shared/", "");
                await HandleSharedList(shareId);
            }
        }

        private async Task HandleSharedList(string shareId)
        {
            var popup = new SharedShoppingListInvitationPopup(_shoppingListsStore, shareId);
            await MopupService.Instance.PushAsync(popup);
        }
    }
}
