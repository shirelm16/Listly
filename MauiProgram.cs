using Listly.Service;
using Listly.Store;
using Listly.View;
using Listly.ViewModel;
using Microsoft.Extensions.Logging;
#if ANDROID
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
#endif
using Mopups.Hosting;

namespace Listly
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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
            builder.Services.AddSingleton<ISQLiteService, SQLiteService>(povider =>
            {
                var sqliteService = new SQLiteService();
                Task.Run(async () => await sqliteService.InitializeAsync()).Wait();
                return sqliteService;
            });

            builder.Services.AddSingleton<ShoppingListStore>();
            builder.Services.AddSingleton<ShoppingItemStore>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<ShoppingListDetailsPage>();
            builder.Services.AddSingleton<ShoppingListsViewModel>();
            builder.Services.AddScoped<ShoppingListDetailsViewModel>();


            return builder.Build();
        }
    }
}
