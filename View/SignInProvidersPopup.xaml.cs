using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class SignInProvidersPopup : PopupPage
{
	public SignInProvidersPopup(ProfileViewModel viewModel)
	{
        InitializeComponent();
        BindingContext = viewModel;
    }
}