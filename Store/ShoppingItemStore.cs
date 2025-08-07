using Listly.Model;
using Listly.Service;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public interface IShoppingItemStore
    {
        Task CreateAsync(ShoppingItem item);
        Task<List<ShoppingItem>> GetByShoppingListIdAsync(Guid shoppingListId);
        Task DeleteAsync(Guid id);
        Task UpdateAsync(ShoppingItem shoppingItem);
    }

    public class ShoppingItemStore : IShoppingItemStore
    {
        private readonly SQLiteAsyncConnection _db;

        public ShoppingItemStore(ISQLiteService dbService)
        {
            _db = dbService.GetConnection();
        }

        public async Task Init()
        {
            await _db.CreateTableAsync<ShoppingItem>();
        }
        
        public async Task CreateAsync(ShoppingItem shoppingItem)
        {
            await _db.InsertAsync(shoppingItem);
        }

        public async Task<List<ShoppingItem>> GetByShoppingListIdAsync(Guid listId)
        {
            return await _db.Table<ShoppingItem>()
                            .Where(i => i.ShoppingListId == listId).ToListAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            await _db.DeleteAsync<ShoppingItem>(id);
        }

        public async Task UpdateAsync(ShoppingItem shoppingItem)
        {
            await _db.UpdateAsync(shoppingItem);
        }
    }
}
