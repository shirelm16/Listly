using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Services
{
    public enum SortOption
    {
        Category
    }
    public interface IShoppingItemsSortingService
    {
        IEnumerable<ShoppingItem> Sort(IEnumerable<ShoppingItem> items, SortOption sortOption = SortOption.Category);
    }

    public class ShoppingItemsSortingService : IShoppingItemsSortingService
    {
        public IEnumerable<ShoppingItem> Sort(IEnumerable<ShoppingItem> items, SortOption sortOption = SortOption.Category)
        {
            return items.OrderBy(x => x.Category.Name);
        }
    }
}
