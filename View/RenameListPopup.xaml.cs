using Listly.Model;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class RenameListPopup : PopupPage
{
	public RenameListPopup(ShoppingList shoppingList)
	{
		InitializeComponent();
        BindingContext = new RenameListPopupViewModel(shoppingList);
    }
}