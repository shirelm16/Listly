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
    public class ShoppingListStore
    {
        private readonly SQLiteAsyncConnection _db;

        public ShoppingListStore(ISQLiteService dbService)
        {
            _db = dbService.GetConnection();
        }

        public async Task Init()
        {
            await _db.CreateTableAsync<ShoppingList>();
        }

        public async Task AddShoppingList(ShoppingList shoppingList)
        {
            await _db.InsertAsync(shoppingList);
        }

        public async Task UpdateShoppingList(ShoppingList shoppingList)
        {
            await _db.UpdateAsync(shoppingList);
        }

        public async Task<List<ShoppingList>> GetShoppingLists()
        {
            var lists = await _db.Table<ShoppingList>().ToListAsync();

            foreach (var list in lists)
            {
                var items = await _db.Table<ShoppingItem>()
                                      .Where(item => item.ShoppingListId == list.Id)
                                      .ToListAsync();
                foreach (var item in items)
                    list.Items.Add(item);
            }

            return lists;
        }

        public async Task RemoveShoppingList(Guid id)
        {
            await _db.DeleteAsync<ShoppingList>(id);
        }
    }
}
