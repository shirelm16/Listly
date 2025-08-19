using Listly.Model;
using Listly.Store;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class ShareShoppingListPopup : PopupPage
{
	public ShareShoppingListPopup(IShoppingListStore shoppingListStore, ShoppingList shoppingList)
	{
		InitializeComponent();
		BindingContext = new ShareShoppingListpPopupViewModel(shoppingListStore, shoppingList);
	}
}