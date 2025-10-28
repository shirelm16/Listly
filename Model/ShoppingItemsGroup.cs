using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Model
{
    public class ShoppingItemsGroup : ObservableCollection<ShoppingItem>, INotifyPropertyChanged
    {
        private string _title;

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            private set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsExpandable { get; }


        public ShoppingItemsGroup(string title, IEnumerable<ShoppingItem> items, bool isExpanded = false, bool isExpandable = false)
            : base(items)
        {
            Title = title;
            IsExpandable = isExpandable;
            IsExpanded = isExpanded;

            if (IsExpandable && !IsExpanded)
                Clear();
        }

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        public void SetExpanded(bool expanded)
        {
            if (!IsExpandable) return;

            IsExpanded = expanded;
        }

        public void RefreshItems(List<ShoppingItem> items)
        {
            Clear();
            foreach (var item in items)
                Add(item);
        }
    }
}
