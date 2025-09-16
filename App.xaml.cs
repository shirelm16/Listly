using Listly.Store;
using Listly.View;
using Mopups.Services;
using Plugin.Firebase.Auth;
using Plugin.Firebase.CloudMessaging;

namespace Listly
{
    public partial class App : Application
    {
        private readonly IShoppingListStore _shoppingListsStore;
        private readonly IUsersStore _usersStore;
        private readonly IFirebaseAuth _auth;

        public App(IFirebaseAuth auth, IShoppingListStore shoppingListsStore, IUsersStore usersStore)
        {
            InitializeComponent();
            _shoppingListsStore = shoppingListsStore;
            _usersStore = usersStore;
            _auth = auth;
            MainPage = new AppShell();

            Task.Run(InitializeUserAsync);
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

        private async Task InitializeUserAsync()
        {
            var user = _auth.CurrentUser;

            if (user == null)
            {
                user = await _auth.SignInAnonymouslyAsync();
            }

            var existingUser = await _usersStore.GetUser(user.Uid);
            if (existingUser == null)
            {
                await _usersStore.CreateUser(user.Uid);
            }

            if (!user.IsAnonymous)
            {
                CrossFirebaseCloudMessaging.Current.TokenChanged += async (s, newToken) =>
                {
                    var user = _auth.CurrentUser;
                    if (user != null)
                    {
                        await _usersStore.UpdateDeviceToken(user.Uid, newToken.Token);
                    }
                };
            }
        }
    }
}
