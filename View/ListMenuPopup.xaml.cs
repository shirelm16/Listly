using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listly.Model;
using Listly.Store;
using Listly.ViewModel;
using Mopups.Pages;
using Mopups.Services;

namespace Listly.View;

public partial class ListMenuPopup : PopupPage
{
    private readonly ShoppingList _selectedList;
    private readonly ShoppingListsViewModel _viewModel;

    public ListMenuPopup(ShoppingList selectedList, ShoppingListsViewModel viewModel)
	{
		InitializeComponent();  
        _selectedList = selectedList;
        _viewModel = viewModel;
    }

    [RelayCommand]
    async Task Delete()
    {
        await _viewModel.DeleteListCommand.ExecuteAsync(_selectedList);
        await MopupService.Instance.PopAsync();
    }

    [RelayCommand]
    async Task Rename()
    {
        var popup = new RenameListPopup(_selectedList);
        await MopupService.Instance.PopAsync();
        await MopupService.Instance.PushAsync(popup);
    }
}