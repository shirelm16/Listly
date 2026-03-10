using Listly.Model;
using Listly.Services;
using Listly.Store;
using Listly.ViewModel;
using Mopups.Pages;

namespace Listly.View;

public partial class AddEditShoppingItemPopup : PopupPage
{
	public AddEditShoppingItemPopup(IShoppingItemStore shoppingItemStore, IUsersStore usersStore, ICurrentUserService currentUserService, ICategorySuggestionService categorySuggestionService, Guid ShoppingListId, ShoppingItem shoppingItem = null)
	{
		InitializeComponent();
		BindingContext = new AddEditShoppingItemViewModel(shoppingItemStore, usersStore, currentUserService, categorySuggestionService, shoppingItem, ShoppingListId);
    }

    private CancellationTokenSource? _glimmerCts;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartGlimmerAnimation();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _glimmerCts?.Cancel();
    }

    private async void StartGlimmerAnimation()
    {
        _glimmerCts?.Cancel();
        _glimmerCts = new CancellationTokenSource();
        var token = _glimmerCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                // Fade aura in
                await SparkleAura.FadeTo(0.35, 800, Easing.SinInOut);
                // Hold briefly at peak glow
                await Task.Delay(200, token);
                // Fade aura out
                await SparkleAura.FadeTo(0, 800, Easing.SinInOut);
                // Pause before next pulse
                await Task.Delay(100, token);
            }
        }
        catch (OperationCanceledException)
        {
            // expected when popup closes
        }
    }
}