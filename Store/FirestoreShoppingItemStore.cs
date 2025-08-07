using Google.Cloud.Firestore;
using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Store
{
    public class FirestoreShoppingItemStore : IShoppingItemStore
    {
        private readonly FirestoreDb _db;
        private readonly CollectionReference _collection;

        public FirestoreShoppingItemStore(FirestoreDb db)
        {
            _db = db;
            _collection = _db.Collection("shoppingItems");
        }

        public async Task CreateAsync(ShoppingItem item)
        {
            var doc = ShoppingItemDocument.FromShoppingItem(item);
            var docRef = _collection.Document(item.Id.ToString());

            await docRef.SetAsync(doc);
        }

        public async Task DeleteAsync(Guid id)
        {
            var docRef = _collection.Document(id.ToString());
            await docRef.DeleteAsync();
            await DeleteAllByShoppingListIdAsync(id);
        }

        public async Task<List<ShoppingItem>> GetByShoppingListIdAsync(Guid shoppingListId)
        {
            var query = _collection.WhereEqualTo("shoppingListId", shoppingListId.ToString());
            var snapshot = await query.GetSnapshotAsync();
            var items = new List<ShoppingItem>();

            foreach (var doc in snapshot.Documents)
            {
                var itemDoc = doc.ConvertTo<ShoppingItemDocument>();
                items.Add(itemDoc.ToShoppingItem());
            }

            return items;
        }

        public async Task UpdateAsync(ShoppingItem shoppingItem)
        {
            var doc = ShoppingItemDocument.FromShoppingItem(shoppingItem);
            var docRef = _collection.Document(shoppingItem.Id.ToString());

            await docRef.SetAsync(doc);
        }

        private async Task DeleteAllByShoppingListIdAsync(Guid shoppingListId)
        {
            var query = _collection.WhereEqualTo("shoppingListId", shoppingListId.ToString());
            var snapshot = await query.GetSnapshotAsync();

            var batch = _db.StartBatch();
            var batchSize = 0;

            foreach (var doc in snapshot.Documents)
            {
                batch.Delete(doc.Reference);
                batchSize++;

                // Firestore batch limit is 500 operations
                if (batchSize >= 500)
                {
                    await batch.CommitAsync();
                    batch = _db.StartBatch();
                    batchSize = 0;
                }
            }

            if (batchSize > 0)
            {
                await batch.CommitAsync();
            }
        }
    }
}
