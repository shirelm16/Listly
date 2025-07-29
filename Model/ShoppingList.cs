using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Model
{
    public partial class ShoppingList : ObservableObject
    {
        [PrimaryKey]
        public Guid Id { get; set; }

        [ObservableProperty]
        string name;

        [Ignore]
        public List<ShoppingItem> Items { get; set; } = new();

        public double Progress
        {
            get
            {
                if (Items == null || Items.Count == 0)
                    return 0;

                int purchased = Items.Count(i => i.IsPurchased);
                return (double)purchased / Items.Count;
            }
        }

        public string ProgressLabel => $"{Items.Count(i => i.IsPurchased)}/{Items.Count}";
    }
}
