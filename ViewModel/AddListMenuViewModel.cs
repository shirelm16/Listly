using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.ViewModel
{
    public class AddListMenuViewModel
    {
        private readonly Func<Task> _onNewList;
        private readonly Func<Task> _onFromRecipe;

        public AddListMenuViewModel(Func<Task> onNewList, Func<Task> onFromRecipe)
        {
            _onNewList = onNewList;
            _onFromRecipe = onFromRecipe;
            NewListCommand = new AsyncRelayCommand(onNewList);
            FromRecipeCommand = new AsyncRelayCommand(onFromRecipe);
        }

        public IAsyncRelayCommand NewListCommand { get; }
        public IAsyncRelayCommand FromRecipeCommand { get; }
    }
}
