using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Plugin.Firebase.Auth.Facebook;
using Plugin.Firebase.Auth.Google;

namespace Listly
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "https",
        DataHost = Consts.AppHost,
        AutoVerify = true,
        DataPathPrefix ="/shared")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleIntent(Intent);

            var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            FirebaseAuthGoogleImplementation.HandleActivityResultAsync(requestCode, resultCode, data);
            FirebaseAuthFacebookImplementation.HandleActivityResultAsync(requestCode, resultCode, data);
        }

        private void HandleIntent(Intent intent)
        {
            var dataString = intent?.DataString;
            if (!string.IsNullOrEmpty(dataString))
            {
                App.Current.SendOnAppLinkRequestReceived(new Uri(dataString));
            }
        }
    }
}
