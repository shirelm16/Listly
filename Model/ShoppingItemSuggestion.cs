using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Model
{
    public partial class ShoppingItemSuggestion : ObservableObject
    {
        [ObservableProperty]
        string name;

        [ObservableProperty]
        double? quantity;

        [ObservableProperty]
        string unit;

        [ObservableProperty]
        string suggestedCategory;

        [ObservableProperty]
        bool isSelected;

        public ShoppingItemSuggestion(string name, double? quantity, string unit, string category)
        {
            Name = name;
            Quantity = quantity;
            Unit = unit;
            SuggestedCategory = category;
        }
    }
}
