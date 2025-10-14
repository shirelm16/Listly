using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Listly.Model;
using Listly.View;
using Mopups.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public partial class SortOptionsPopupViewModel : BaseViewModel
    {
        private ShoppingListDetailsViewModel parentViewModel;

        [ObservableProperty]
        private bool isCategorySelected = false;

        [ObservableProperty]
        private bool isPrioritySelected = false;

        private Action<SortType> _onSortSelected;

        public SortOptionsPopupViewModel(ShoppingListDetailsViewModel parentViewModel, Action<SortType> onSortSelected)
        {
            this.parentViewModel = parentViewModel;
            switch (parentViewModel.ShoppingList.SortType)
            {
                case SortType.Priority:
                    isPrioritySelected = true;
                    break;
                default:
                    isCategorySelected = true;
                    break;
            }

            _onSortSelected = onSortSelected;
        }

        [RelayCommand]
        public void SelectPrioritySort()
        {
            if (IsPrioritySelected)
                return;
            IsPrioritySelected = true;
            IsCategorySelected = false;
            _onSortSelected(SortType.Priority);
        }

        [RelayCommand]
        public void SelectCategorySort()
        {
            if (IsCategorySelected)
                return;
            IsCategorySelected = true;
            IsPrioritySelected = false;
            _onSortSelected(SortType.Category);
        }

        [RelayCommand]
        public async Task Back()
        {
            await MopupService.Instance.PopAsync();
            var menuPopup = new ShoppingListDetailsMenuPopup(parentViewModel);
            await MopupService.Instance.PushAsync(menuPopup);
        }

        [RelayCommand]
        public async Task Close()
        {
            await MopupService.Instance.PopAsync();
        }
    }
}
