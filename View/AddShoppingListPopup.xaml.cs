using Listly.Model;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class AddShoppingListPopup : PopupPage
{
	public AddShoppingListPopup()
	{
		InitializeComponent();
        BindingContext = new AddShoppingListPopupViewModel();
    }
}