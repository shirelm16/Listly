using Google.Cloud.Firestore;
using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public class FirestoreShoppingListStore : IShoppingListStore
    {
        private readonly FirestoreDb _db;
        private readonly CollectionReference _collection;
        public FirestoreShoppingListStore(FirestoreDb db)
        {
            _db = db;
            _collection = _db.Collection("shoppingLists");
        }

        public async Task CreateAsync(ShoppingList shoppingList)
        {
            var doc = ShoppingListDocument.FromShoppingList(shoppingList);
            var docRef = _collection.Document(shoppingList.Id.ToString());

            await docRef.SetAsync(doc);
        }

        public async Task DeleteAsync(Guid id)
        {
            var docRef = _collection.Document(id.ToString());
            await docRef.DeleteAsync();
        }

        public async Task<List<ShoppingList>> GetAllAsync()
        {
            var snapshot = await _collection.GetSnapshotAsync();
            var lists = new List<ShoppingList>();

            foreach (var doc in snapshot.Documents)
            {
                var listDoc = doc.ConvertTo<ShoppingListDocument>();
                lists.Add(listDoc.ToShoppingList());
            }

            return lists;
        }

        public async Task UpdateAsync(ShoppingList shoppingList)
        {
            var doc = ShoppingListDocument.FromShoppingList(shoppingList);
            var docRef = _collection.Document(shoppingList.Id.ToString());

            await docRef.SetAsync(doc);
        }
    }
}
