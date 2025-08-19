using Listly.Store;
using Listly.ViewModel;
using Mopups.Pages;
using System.Collections.Specialized;

namespace Listly.View;

public partial class SharedShoppingListInvitationPopup : PopupPage
{
	public SharedShoppingListInvitationPopup(IShoppingListStore shoppingListStore, string shareId)
	{
		InitializeComponent();
		BindingContext = new SharedShoppingListInvitationViewModel(shoppingListStore, shareId);
	}
}