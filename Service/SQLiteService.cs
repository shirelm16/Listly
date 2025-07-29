using Listly.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Service
{
    public interface ISQLiteService
    {
        SQLiteAsyncConnection GetConnection();
    }

    public class SQLiteService : ISQLiteService
    {

        private SQLiteAsyncConnection _connection;

        public SQLiteAsyncConnection GetConnection() => _connection;

        public async Task InitializeAsync()
        {
            if (_connection != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shopping.db");
            _connection = new SQLiteAsyncConnection(dbPath);

            await _connection.CreateTableAsync<ShoppingList>();
            await _connection.CreateTableAsync<ShoppingItem>();
        }
    }
}
