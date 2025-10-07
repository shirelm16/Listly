using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listly.Model;
using Listly.Store;
using Listly.View;
using Mopups.Services;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Google;
using Plugin.Firebase.CloudMessaging;

namespace Listly.ViewModel
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IFirebaseAuth _auth;
        private readonly IFirebaseAuthGoogle _googleAuth;
        private readonly IUsersStore _usersStore;

        public ProfileViewModel(IFirebaseAuth auth, 
            IFirebaseAuthGoogle googleAuth, 
            IUsersStore usersStore)
        {
            _auth = auth;
            _googleAuth = googleAuth;
            _usersStore = usersStore;
            UpdateUserState();
        }

        [ObservableProperty]
        private string welcomeText = "Welcome!";

        [ObservableProperty]
        private bool isSignedIn;

        [ObservableProperty]
        private bool isSignedOut;

        [ObservableProperty]
        private bool isLoading;

        private void UpdateUserState()
        {
            var user = _auth.CurrentUser;
            if (user != null && !user.IsAnonymous)
            {
                WelcomeText = $"Welcome {user.DisplayName ?? user.Email ?? "User"}!";
                IsSignedIn = true;
                IsSignedOut = false;
            }
            else
            {
                WelcomeText = "Welcome! Sign in to save your data.";
                IsSignedIn = false;
                IsSignedOut = true;
            }
        }

        [RelayCommand]
        private async Task ShowProvidersAction()
        {
            await MopupService.Instance.PushAsync(new SignInProvidersPopup(this));
        }

        [RelayCommand]
        public async Task SignInWithGoogle()
        {
            IsLoading = true;

            try
            {
                await _googleAuth.SignInWithGoogleAsync();
                await CreateOrUpdateUserInDb();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateUserState();
                    MopupService.Instance.PopAsync();
                });
            }
            catch (Plugin.Firebase.Core.Exceptions.FirebaseAuthException ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected error during Google sign-in: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred. Please try again.", "OK");
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SignOut()
        {
            try
            {
                await _auth.SignOutAsync();
                await _auth.SignInAnonymouslyAsync();
                UpdateUserState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign out error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LogOut()
        {
            await SignOut();
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await MopupService.Instance.PopAsync();
        }

        private async Task CreateOrUpdateUserInDb()
        {
            var user = _auth.CurrentUser;
            if (user != null)
            {
                var userData = new User
                {
                    Id = user.Uid,
                    Email = user.Email,
                    Name = user.DisplayName
                };

                var deviceToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
                if (deviceToken != null)
                {
                    userData.DeviceToken = deviceToken;
                }
                await _usersStore.CreateOrUpdateUser(userData);
            }
        }
    }
}