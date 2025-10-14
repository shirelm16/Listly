using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class SortOptionsPopup : PopupPage
{
	public SortOptionsPopup(SortOptionsPopupViewModel viewModel)
	{
        BindingContext = viewModel;
        InitializeComponent();
	}
}