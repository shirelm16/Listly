using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Services
{
    public interface IBucketSortService<TItem>
    {
        void AddItem(TItem item);
        void RemoveItem(TItem item);
        void UpdateItem(TItem item);
        IEnumerable<TItem> GetSortedItems();
        void Clear();
    }

    public class BucketSortService<TItem, TKey> : IBucketSortService<TItem>
        where TItem : class
        where TKey : notnull
    {
        private readonly Dictionary<TKey, ObservableCollection<TItem>> _buckets = new();
        private readonly Dictionary<TItem, TKey> _itemToKey = new();
        private readonly Func<TItem, TKey> _keySelector;
        private readonly IEnumerable<TKey> _orderedKeys;

        public BucketSortService(Func<TItem, TKey> keySelector, IEnumerable<TKey> orderedKeys)
        {
            _keySelector = keySelector;
            _orderedKeys = orderedKeys;

            foreach (var key in orderedKeys)
            {
                _buckets[key] = new ObservableCollection<TItem>();
            }
        }

        public void AddItem(TItem item)
        {
            var key = _keySelector(item);
            if (_buckets.TryGetValue(key, out var bucket))
            {
                bucket.Add(item);
                _itemToKey[item] = key;
            }
        }

        public void RemoveItem(TItem item)
        {
            if (_itemToKey.TryGetValue(item, out var key) &&
                _buckets.TryGetValue(key, out var bucket))
            {
                bucket.Remove(item);
                _itemToKey.Remove(item);
            }
        }

        public void UpdateItem(TItem item)
        {
            var newKey = _keySelector(item);

            if (_itemToKey.TryGetValue(item, out var oldKey) &&
                !EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
            {
                RemoveItem(item);
                AddItem(item);
            }
        }

        public IEnumerable<TItem> GetSortedItems()
        {
            return _orderedKeys.SelectMany(key => _buckets[key]);
        }

        public void Clear()
        {
            foreach (var bucket in _buckets.Values)
            {
                bucket.Clear();
            }
            _itemToKey.Clear();
        }
    }
}
