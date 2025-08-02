using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public ObservableCollection<ShoppingItem> Items { get; set; } = new();

        [Ignore]
        public int ItemCount => Items.Count;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LastModifiedLocal))]
        DateTime lastModified = DateTime.UtcNow;

        [Ignore]
        public DateTime LastModifiedLocal => LastModified.ToLocalTime();

        public ShoppingList() { }

        public ShoppingList(string name)
        {
            Name = name;
        }
    }
}
