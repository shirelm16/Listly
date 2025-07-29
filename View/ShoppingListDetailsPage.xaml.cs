using Listly.ViewModel;

namespace Listly.View;

public partial class ShoppingListDetailsPage : ContentPage
{
	public ShoppingListDetailsPage(ShoppingListDetailsViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}