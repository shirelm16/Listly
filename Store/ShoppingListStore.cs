﻿using Listly.Model;
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
            var allItems = await _db.Table<ShoppingItem>().ToListAsync();
            var itemsGroupedByList = allItems.GroupBy(item => item.ShoppingListId)
                                          .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var list in lists)
            {
                if (itemsGroupedByList.TryGetValue(list.Id, out var itemsForThisList))
                {
                    foreach (var item in itemsForThisList)
                        list.Items.Add(item);
                }
            }

            return lists;
        }

        public async Task RemoveShoppingList(Guid id)
        {
            await _db.Table<ShoppingItem>().Where(item => item.ShoppingListId == id).DeleteAsync();
            await _db.DeleteAsync<ShoppingList>(id);
        }
    }
}
