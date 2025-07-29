using Listly.View;

namespace Listly
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(ShoppingListDetailsPage), typeof(ShoppingListDetailsPage));
        }
    }
}
