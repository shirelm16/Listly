using Listly.Model;
using Plugin.Firebase.Auth;
using Plugin.Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Listly.Store
{
    public interface IShoppingListStore
    {
        Task CreateShoppingListAsync(ShoppingList shoppingList);
        Task UpdateShoppingListAsync(ShoppingList shoppingList);
        Task<List<ShoppingList>> GetAllShoppingListsAsync();
        Task DeleteListAsync(Guid id);
        Task<ShoppingList> GetSharedListByShareIdAsync(string shareId);
        Task AddCurrentUserAsCollaboratorOfShoppingList(ShoppingList shoppingList);
    }

    public interface IShoppingItemStore
    {
        Task CreateShoppingItemAsync(ShoppingItem item);
        Task CreateShoppingItemsBatchAsync(IEnumerable<ShoppingItem> items);
        Task DeleteShoppingItemAsync(Guid shoppingListId, Guid itemId);
        Task UpdateShoppingItemAsync(ShoppingItem shoppingItem, string? changedProperty = null);
        IDisposable ListenToItems(Guid listId, Action<IEnumerable<ShoppingItem>> onChanged);
    }

    public class FirestoreShoppingListStore : IShoppingListStore, IShoppingItemStore
    {
        private readonly ICollectionReference _collection;
        private IFirebaseAuth _auth;

        // Maps C# property names to Firestore field names for field-level writes.
        // Only properties that map directly to a single stored field are included;
        // anything absent falls back to a full-document write.
        private static readonly Dictionary<string, string> ItemFieldMap = new()
        {
            [nameof(ShoppingItem.Name)]        = "name",
            [nameof(ShoppingItem.Quantity)]    = "quantity",
            [nameof(ShoppingItem.Unit)]        = "unit",
            [nameof(ShoppingItem.IsPurchased)] = "isPurchased",
            [nameof(ShoppingItem.Category)]    = "category",
            [nameof(ShoppingItem.HasPriority)] = "has_priority",
            [nameof(ShoppingItem.Priority)]    = "priority",
        };

        public FirestoreShoppingListStore(IFirebaseAuth auth)
        {
            _auth = auth;
            _collection = CrossFirebaseFirestore.Current.GetCollection("shoppingLists");
        }

        public async Task CreateShoppingListAsync(ShoppingList shoppingList)
        {
            var user = _auth.CurrentUser;
            
            if (string.IsNullOrEmpty(user?.Uid))
                throw new InvalidOperationException("User must be authenticated");

            shoppingList.OwnerId = user.Uid;
            shoppingList.LastModifiedUser = user.Uid;
            var doc = ShoppingListDocument.FromShoppingList(shoppingList);
            var docRef = _collection.GetDocument(shoppingList.Id.ToString());

            await docRef.SetDataAsync(doc);
        }

        public async Task CreateShoppingItemAsync(ShoppingItem item)
        {
            var itemsCollection = _collection
                .GetDocument(item.ShoppingListId.ToString())
                .GetCollection("items");

            var doc = ShoppingItemDocument.FromShoppingItem(item);
            await itemsCollection.GetDocument(item.Id.ToString()).SetDataAsync(doc);
        }

        public async Task DeleteListAsync(Guid id)
        {
            var docRef = _collection.GetDocument(id.ToString());
            await docRef.DeleteDocumentAsync();
        }

        public async Task DeleteShoppingItemAsync(Guid shoppingListId, Guid itemId)
        {
            var itemRef = _collection
                .GetDocument(shoppingListId.ToString())
                .GetCollection("items")
                .GetDocument(itemId.ToString());

            await itemRef.DeleteDocumentAsync();
        }

        public async Task<List<ShoppingList>> GetAllShoppingListsAsync()
        {
            var user = _auth.CurrentUser;
            if (string.IsNullOrEmpty(user?.Uid))
                return [];

            var ownedListsSnapshot = await _collection
                .WhereEqualsTo("ownerId", user.Uid)
                .GetDocumentsAsync<ShoppingListDocument>();

            var collaboratorListsSnapshot = await _collection
                .WhereArrayContains("collaborators", user.Uid)
                .GetDocumentsAsync<ShoppingListDocument>();

            var allDocuments = ownedListsSnapshot.Documents
                .Concat(collaboratorListsSnapshot.Documents)
                .GroupBy(doc => doc.Data.Id)
                .Select(group => group.First())
                .ToList();

            var shoppingLists = new List<ShoppingList>();

            var fetchTasks = allDocuments.Select(async doc =>
            {
                return await GetShoppingListWithItems(doc);
            });

            return (await Task.WhenAll(fetchTasks)).ToList();
        }

        public async Task UpdateShoppingListAsync(ShoppingList shoppingList)
        {
            var user = _auth.CurrentUser;

            if (string.IsNullOrEmpty(user?.Uid))
                throw new InvalidOperationException("User must be authenticated");

            shoppingList.LastModifiedUser = user.Uid;
            var doc = ShoppingListDocument.FromShoppingList(shoppingList);
            var docRef = _collection.GetDocument(shoppingList.Id.ToString());

            await docRef.SetDataAsync(doc);
        }

        public async Task UpdateShoppingItemAsync(ShoppingItem shoppingItem, string? changedProperty = null)
        {
            var itemRef = _collection
                .GetDocument(shoppingItem.ShoppingListId.ToString())
                .GetCollection("items")
                .GetDocument(shoppingItem.Id.ToString());

            var doc = ShoppingItemDocument.FromShoppingItem(shoppingItem);

            if (changedProperty != null && ItemFieldMap.TryGetValue(changedProperty, out var firestoreField))
                await itemRef.SetDataAsync(doc, SetOptions.MergeFields(firestoreField));
            else
                await itemRef.SetDataAsync(doc, SetOptions.Merge());
        }

        public async Task<ShoppingList> GetSharedListByShareIdAsync(string shareId)
        {
            var query = await _collection
                    .WhereEqualsTo("shareId", shareId)
                    .GetDocumentsAsync<ShoppingListDocument>();

            var sharedListDoc = query.Documents.FirstOrDefault();
            
            if (sharedListDoc == null || sharedListDoc.Data.ToShoppingList().ShareExpiresAt < DateTime.UtcNow)
                return null;


            return await GetShoppingListWithItems(sharedListDoc);
        }

        public async Task AddCurrentUserAsCollaboratorOfShoppingList(ShoppingList shoppingList)
        {
            var user = _auth.CurrentUser;

            if (string.IsNullOrEmpty(user?.Uid))
                throw new InvalidOperationException("User must be authenticated");

            if (shoppingList.OwnerId != user.Uid && !shoppingList.Collaborators.Contains(user.Uid))
            {
                shoppingList.Collaborators.Add(user.Uid);
                await UpdateShoppingListAsync(shoppingList);
            }
        }

        private async Task<ShoppingList> GetShoppingListWithItems(IDocumentSnapshot<ShoppingListDocument> doc)
        {
            var shoppingList = doc.Data.ToShoppingList();

            var itemsCollection = doc.Reference.GetCollection("items");
            var itemsSnapshot = await itemsCollection.GetDocumentsAsync<ShoppingItemDocument>();

            shoppingList.Items.Clear();
            foreach (var itemDoc in itemsSnapshot.Documents)
            {
                var item = itemDoc.Data.ToShoppingItem();
                shoppingList.Items.Add(item);
            }
            return shoppingList;
        }

        public IDisposable ListenToItems(Guid listId, Action<IEnumerable<ShoppingItem>> onChanged)
        {
            return _collection
                .GetDocument(listId.ToString())
                .GetCollection("items")
                .AddSnapshotListener<ShoppingItemDocument>((snapshot) =>
                {
                    var items = snapshot.Documents
                        .Select(doc => doc.Data.ToShoppingItem())
                        .ToList();
                    onChanged(items);
                });
        }

        public async Task CreateShoppingItemsBatchAsync(IEnumerable<ShoppingItem> items)
        {
            var batch = CrossFirebaseFirestore.Current.CreateBatch();
            foreach (var item in items)
            {
                var doc = _collection
                    .GetDocument(item.ShoppingListId.ToString())
                    .GetCollection("items")
                    .GetDocument(item.Id.ToString());
                batch.SetData(doc, ShoppingItemDocument.FromShoppingItem(item));
            }
            await batch.CommitAsync();
        }
    }
}
