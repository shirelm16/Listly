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

            Task.Run(() => InitializeUserAsync(authService));
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

        private async Task InitializeUserAsync(IAuthService authService)
        {
            await authService.SignInAnonymouslyAsync();
            var userId = await authService.GetCurrentUserIdAsync();

            var existingUser = await _usersStore.GetUser(userId);
            if (existingUser == null)
            {
                await _usersStore.CreateUser(userId);
            }

            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                await _usersStore.AddDeviceToken(userId, token);
            }

            CrossFirebaseCloudMessaging.Current.TokenChanged += async (s, newToken) =>
            {
                await _usersStore.AddDeviceToken(userId, newToken.Token);
            };
        }
    }
}
