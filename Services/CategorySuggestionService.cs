using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Services
{

    public interface ICategorySuggestionService
    {
        public Task<Category?> SuggestCategoryAsync(string itemName, string userId);
    }

    public class CategorySuggestionService : ICategorySuggestionService
    {
        private readonly HttpClient _httpClient;

        public CategorySuggestionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Category?> SuggestCategoryAsync(string itemName, string userId)
        {
            var request = new
            {
                ItemName = ItemNameNormalizer.Normalize(itemName),
                UserId = userId,
                categories = Enum.GetNames(typeof(Category))
            };

            var response = await _httpClient.PostAsJsonAsync("https://categorizeitem-tjbk5gsn3q-uc.a.run.app", request);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<Response>();

            if (result == null)
                return null;

            if (Enum.TryParse<Category>(result.Category, out var category))
                return category;

            return null;
        }

        private class Response
        {
            public string Category { get; set; }
            public string Source { get; set; }
        }
    }
}
