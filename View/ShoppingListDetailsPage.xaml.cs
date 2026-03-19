using Listly.ViewModel;

namespace Listly.View;

public partial class ShoppingListDetailsPage : ContentPage
{
	public ShoppingListDetailsPage(ShoppingListDetailsViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }

#if IOS
    private UIKit.UITapGestureRecognizer? _keyboardDismissTap;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var window = UIKit.UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIKit.UIWindowScene>()
            .SelectMany(s => s.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);

        if (window != null)
        {
            _keyboardDismissTap = new UIKit.UITapGestureRecognizer(() => SearchEntry.Unfocus())
            {
                CancelsTouchesInView = false
            };
            window.AddGestureRecognizer(_keyboardDismissTap);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_keyboardDismissTap != null)
        {
            _keyboardDismissTap.View?.RemoveGestureRecognizer(_keyboardDismissTap);
            _keyboardDismissTap = null;
        }
    }
#endif
}