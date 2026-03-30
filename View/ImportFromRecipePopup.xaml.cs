using Listly.Services;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class ImportFromRecipePopup : PopupPage
{
	public ImportFromRecipePopup(IImportFromRecipeService importFromRecipeService, Guid? shoppingListId = null)
	{
		InitializeComponent();
		BindingContext = new ImportFromRecipePopupViewModel(importFromRecipeService, shoppingListId);
	}
}