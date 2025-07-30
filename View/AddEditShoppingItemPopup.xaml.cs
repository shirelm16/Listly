using Listly.Model;
using Listly.Store;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class AddEditShoppingItemPopup : PopupPage
{
	public AddEditShoppingItemPopup(string title, ShoppingItemStore shoppingItemStore, Guid ShoppingListId, ShoppingItem shoppingItem = null)
	{
		InitializeComponent();
		BindingContext = new AddEditShoppingItemViewModel(shoppingItemStore, shoppingItem, ShoppingListId);
		Title = title;
    }
}