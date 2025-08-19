using Listly.Service;
using Listly.Store;
using Listly.View;
using Listly.ViewModel;
using Mopups.Services;

namespace Listly
{
    public partial class App : Application
    {
        private readonly IShoppingListStore _shoppingListsStore;

        public App(IAuthService authService, IShoppingListStore shoppingListsStore)
        {
            InitializeComponent();
            _shoppingListsStore = shoppingListsStore;
            MainPage = new AppShell();

            Task.Run(authService.SignInAnonymouslyAsync);
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
