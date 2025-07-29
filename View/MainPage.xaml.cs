using Listly.ViewModel;

namespace Listly.View;

public partial class MainPage : ContentPage
{
    private readonly ShoppingListsViewModel _viewModel;
    public MainPage(ShoppingListsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Always re-fetch on page load
        if (_viewModel.GetShoppingListsCommand.CanExecute(null))
            await _viewModel.GetShoppingListsCommand.ExecuteAsync(null);
    }

}
