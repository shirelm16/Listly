using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class ShoppingListDetailsMenuPopup : PopupPage
{
	public ShoppingListDetailsMenuPopup(ShoppingListDetailsViewModel viewModel)
	{
        BindingContext = viewModel;
        InitializeComponent();
	}
}