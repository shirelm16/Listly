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
    public class ShoppingItemStore
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

        public async Task AddItemToShoppingList(ShoppingItem shoppingItem)
        {
            await _db.InsertAsync(shoppingItem);
        }

        public async Task<List<ShoppingItem>> GetShoppingListItems(Guid listId)
        {
            return await _db.Table<ShoppingItem>()
                            .Where(i => i.ShoppingListId == listId).ToListAsync();
        }

        public async Task RemoveShoppingItem(Guid id)
        {
            await _db.DeleteAsync<ShoppingItem>(id);
        }

        public async Task UpdateShoppingItem(ShoppingItem shoppingItem)
        {
            await _db.UpdateAsync(shoppingItem);
        }
    }
}
