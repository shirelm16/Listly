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
        Task DeleteShoppingItemAsync(Guid shoppingListId, Guid itemId);
        Task UpdateShoppingItemAsync(ShoppingItem shoppingItem);
    }

    public class FirestoreShoppingListStore : IShoppingListStore, IShoppingItemStore
    {
        private readonly ICollectionReference _collection;
        private IFirebaseAuth _auth;

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

        public async Task UpdateShoppingItemAsync(ShoppingItem shoppingItem)
        {
            var itemRef = _collection
                .GetDocument(shoppingItem.ShoppingListId.ToString())
                .GetCollection("items")
                .GetDocument(shoppingItem.Id.ToString());

            var doc = ShoppingItemDocument.FromShoppingItem(shoppingItem);
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

            shoppingList.Collaborators.Add(user.Uid);
            await UpdateShoppingListAsync(shoppingList);
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
    }
}
