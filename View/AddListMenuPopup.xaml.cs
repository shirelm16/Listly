using CommunityToolkit.Maui.Views;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class AddListMenuPopup : PopupPage
{
	public AddListMenuPopup(AddListMenuViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

    }
}