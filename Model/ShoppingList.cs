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

        [Ignore]
        public int ItemCount => Items.Count;

        [ObservableProperty]
        DateTime lastModified;
    }
}
