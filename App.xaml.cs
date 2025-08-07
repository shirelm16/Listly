using Listly.Service;

namespace Listly
{
    public partial class App : Application
    {
        public App(IAuthService authService)
        {
            InitializeComponent();

            MainPage = new AppShell();

            Task.Run(async () => await authService.SignInAnonymouslyAsync());
        }
    }
}
