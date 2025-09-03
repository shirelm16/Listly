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

        [ObservableProperty]
        string name;

        public ObservableCollection<ShoppingItem> Items { get; set; } = new();

        public int ItemCount => Items.Count;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LastModifiedLocal))]
        DateTime lastModified = DateTime.UtcNow;

        public DateTime LastModifiedLocal => LastModified.ToLocalTime();
        public string LastModifiedUser { get; set; }
        public string OwnerId { get; set; }
        
        public string ShareId { get; set; }
        public DateTime? ShareExpiresAt { get; set; }
        public List<string> Collaborators { get; set; } = new();
        public bool IsShared => !string.IsNullOrEmpty(ShareId);
        public bool IsCollaborated => Collaborators.Any();

        public ShoppingList() { }

        public ShoppingList(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }
    }
}
