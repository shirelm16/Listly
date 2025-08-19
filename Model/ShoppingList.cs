using CommunityToolkit.Mvvm.ComponentModel;
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
        public Guid Id { get; set; }

        public ObservableCollection<string> Collaborators { get; set; } = new();

        [ObservableProperty]
        string name;

        public ObservableCollection<ShoppingItem> Items { get; set; } = new();

        public int ItemCount => Items.Count;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LastModifiedLocal))]
        DateTime lastModified = DateTime.UtcNow;

        public DateTime LastModifiedLocal => LastModified.ToLocalTime();

        public ShoppingList() { }

        public ShoppingList(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }
    }
}
