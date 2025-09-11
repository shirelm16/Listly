using Listly.Service;
using Listly.Store;
using Listly.View;
using Listly.ViewModel;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

#if ANDROID
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Plugin.Firebase.Bundled.Platforms.Android;
#endif
using Mopups.Hosting;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Bundled.Shared;
using Plugin.Firebase.Firestore;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Auth.Google;
using Plugin.Firebase.Auth.Facebook;
using Xamarin.Facebook;

namespace Listly
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .RegisterFirebaseServices()
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler(typeof(Entry), typeof(Microsoft.Maui.Handlers.EntryHandler));
                    Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                    {
                        handler.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
                    });
#endif
                })
                .ConfigureMopups()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<IShoppingListStore, FirestoreShoppingListStore>();
            builder.Services.AddSingleton<IShoppingItemStore, FirestoreShoppingListStore>();
            builder.Services.AddSingleton<IUsersStore, FirestoreUsersStore>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<ProfilePage>();
            builder.Services.AddTransient<ShoppingListDetailsPage>();
            builder.Services.AddSingleton<ShoppingListsViewModel>();
            builder.Services.AddSingleton<ProfileViewModel>();
            builder.Services.AddScoped<ShoppingListDetailsViewModel>();

            return builder.Build();
        }

        private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
        {
            builder.ConfigureLifecycleEvents(events => {
#if ANDROID
                events.AddAndroid(android => android.OnCreate((activity, _) =>
                {
                    CrossFirebase.Initialize(activity, CreateCrossFirebaseSettings());
                }));
#endif
            });

            builder.Services.AddSingleton(_ => CrossFirebaseAuth.Current);
            builder.Services.AddSingleton(_ => CrossFirebaseAuthGoogle.Current);
            builder.Services.AddSingleton(_ => CrossFirebaseAuthFacebook.Current);
            builder.Services.AddSingleton(_ => CrossFirebaseFirestore.Current);
            builder.Services.AddSingleton(_ => CrossFirebaseCloudMessaging.Current);
            builder.Services.AddSingleton<IAuthService, FirebaseAuthService>();
            FirebaseAuthGoogleImplementation.Initialize(Consts.WebClientId);
            return builder;
        }

        private static CrossFirebaseSettings CreateCrossFirebaseSettings()
        {
            return new CrossFirebaseSettings(isAuthEnabled: true, isFirestoreEnabled: true, isCloudMessagingEnabled: true, googleRequestIdToken: Consts.WebClientId);
        }
    }
}
