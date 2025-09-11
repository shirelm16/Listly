using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listly.Model;
using Listly.Store;
using Listly.View;
using Mopups.Services;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Facebook;
using Plugin.Firebase.Auth.Google;

namespace Listly.ViewModel
{
    public partial class ProfileViewModel : BaseViewModel
    {
        private readonly IFirebaseAuth _auth;
        private readonly IFirebaseAuthGoogle _googleAuth;
        private readonly IFirebaseAuthFacebook _facebookAuth;
        private readonly IUsersStore _usersStore;

        public ProfileViewModel(IFirebaseAuth auth, 
            IFirebaseAuthGoogle googleAuth, 
            IFirebaseAuthFacebook firebaseAuth,
            IUsersStore usersStore)
        {
            _auth = auth;
            _googleAuth = googleAuth;
            _facebookAuth = firebaseAuth;
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
            if (user != null)
            {
                if (user.IsAnonymous)
                {
                    WelcomeText = "Welcome! Sign in to save your data.";
                    IsSignedIn = false;
                    IsSignedOut = true;
                }
                else
                {
                    WelcomeText = $"Welcome {user.DisplayName ?? user.Email ?? "User"}!";
                    IsSignedIn = true;
                    IsSignedOut = false;
                }
            }
            else
            {
                WelcomeText = "Welcome!";
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
                var currentUser = _auth.CurrentUser;
                if (currentUser?.IsAnonymous == true)
                {
                    try
                    {
                        await _googleAuth.LinkWithGoogleAsync();
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            UpdateUserState();
                            MopupService.Instance.PopAsync();
                        });
                        return;
                    }
                    catch(Exception)
                    {
                        await _auth.SignOutAsync();
                    }
                }

                await _googleAuth.SignInWithGoogleAsync();

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
                await UpdateUser();
            }
        }

        [RelayCommand]
        public async Task SignInWithFacebook()
        {
            IsLoading = true;

            try
            {
                var currentUser = _auth.CurrentUser;
                if (currentUser?.IsAnonymous == true)
                {
                    await _facebookAuth.LinkWithFacebookAsync();
                }
                else
                {
                    await _facebookAuth.SignInWithFacebookAsync();
                }

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
                System.Diagnostics.Debug.WriteLine($"Unexpected error during Facebook sign-in: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred. Please try again.", "OK");
                });
            }
            finally
            {
                IsLoading = false;
                await UpdateUser();
            }
        }

        [RelayCommand]
        private async Task SignOut()
        {
            try
            {
                await _auth.SignOutAsync();
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

        [RelayCommand]
        public async Task ForceSignOutAndRetry()
        {
            try
            {
                // Nuclear option - completely reset everything
                await _auth.SignOutAsync();
                await Task.Delay(3000); // Wait longer

                // Clear any cached credentials (platform specific)
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Start fresh
                await _auth.SignInAnonymouslyAsync();
                UpdateUserState();

                await Application.Current.MainPage.DisplayAlert("Reset", "Ready to try Google sign-in again", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Force reset error: {ex.Message}");
            }
        }

        private async Task UpdateUser()
        {
            var user = _auth.CurrentUser;
            var userData = new User
            {
                Id = user.Uid,
                Email = user.Email,
                Name = user.DisplayName
            };
            await _usersStore.CreateOrUpdateUser(userData);
        }
    }
}