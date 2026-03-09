using Listly.Model;
using Listly.Services;
using Listly.Store;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class AddEditShoppingItemPopup : PopupPage
{
	public AddEditShoppingItemPopup(IShoppingItemStore shoppingItemStore, ICategorySuggestionService categorySuggestionService, Guid ShoppingListId, ShoppingItem shoppingItem = null)
	{
		InitializeComponent();
		BindingContext = new AddEditShoppingItemViewModel(shoppingItemStore, categorySuggestionService, shoppingItem, ShoppingListId);
    }
}