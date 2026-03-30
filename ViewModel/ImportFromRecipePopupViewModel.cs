using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Listly.Messages;
using Listly.Model;
using Listly.Services;
using Mopups.Services;
using System.Collections.ObjectModel;

namespace Listly.ViewModel
{
    public enum RecipeImportState { EnterUrl, Loading, SelectItems }

    public partial class ImportFromRecipePopupViewModel : BaseViewModel
    {
        private readonly IImportFromRecipeService _importFromRecipeService;
        private readonly Guid? _shoppingListId; // null = new list flow

        public bool IsNewListFlow => _shoppingListId == null;

        [ObservableProperty] 
        RecipeImportState state = RecipeImportState.EnterUrl;
        
        [ObservableProperty] 
        string url = string.Empty;
        
        [ObservableProperty] 
        string errorMessage = string.Empty;
        
        [ObservableProperty] 
        bool hasError;

        public ObservableCollection<ShoppingItemSuggestion> Items { get; } = new();

        public bool CanExtract => !string.IsNullOrWhiteSpace(Url);
        public bool AllSelected => Items.All(i => i.IsSelected);
        public string AddButtonText => IsNewListFlow ? "Create List" : "Add to List";

        public List<string> Categories { get; } = Enum.GetValues<Category>()
            .Select(e => e.GetDisplayWithIcon())
            .ToList();

        public ImportFromRecipePopupViewModel(IImportFromRecipeService importFromRecipeService, Guid? shoppingListId = null)
        {
            _importFromRecipeService = importFromRecipeService;
            _shoppingListId = shoppingListId;
        }

        [RelayCommand]
        void ToggleSelectAll()
        {
            var newValue = !AllSelected;
            foreach (var item in Items)
                item.IsSelected = newValue;
            OnPropertyChanged(nameof(AllSelected));
        }

        [RelayCommand(CanExecute = nameof(CanExtract))]
        async Task Extract()
        {
            HasError = false;
            ErrorMessage = string.Empty;
            State = RecipeImportState.Loading;

            try
            {
                var suggestions = await _importFromRecipeService.ImportFromOnlineRecipeAsync(Url);

                Items.Clear();
                foreach (var item in suggestions)
                {
                    item.IsSelected = true;
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ShoppingItemSuggestion.IsSelected))
                            OnPropertyChanged(nameof(AllSelected));
                    };
                    Items.Add(item);
                }

                if (Items.Count == 0)
                {
                    HasError = true;
                    ErrorMessage = "No ingredients found. Try a different URL.";
                    State = RecipeImportState.EnterUrl;
                    return;
                }

                State = RecipeImportState.SelectItems;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = "Failed to extract recipe. Please check the URL and try again.";
                State = RecipeImportState.EnterUrl;
            }
        }

        [RelayCommand]
        async Task Add()
        {
            var selectedItems = Items.Where(i => i.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                await Shell.Current.DisplayAlert("No Items", "Please select at least one item.", "OK");
                return;
            }

            if (IsNewListFlow)
            {
                // Prompt for list name then create
                var listName = await Shell.Current.DisplayPromptAsync(
                    "New List",
                    "Enter a name for your new shopping list",
                    "Create",
                    "Cancel");

                if (string.IsNullOrWhiteSpace(listName)) return;

                // Send message to create list + add items
                WeakReferenceMessenger.Default.Send(new CreateListFromRecipeMessage(listName, selectedItems));
            }
            else
            {
                // Send message to add items to existing list
                WeakReferenceMessenger.Default.Send(new AddRecipeItemsToListMessage(_shoppingListId!.Value, selectedItems));
            }

            await MopupService.Instance.PopAsync();
        }

        [RelayCommand]
        async Task Cancel() => await MopupService.Instance.PopAsync();

        partial void OnUrlChanged(string value) => ExtractCommand.NotifyCanExecuteChanged();
    }
}