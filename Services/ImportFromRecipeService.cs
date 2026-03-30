
using Listly.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Services
{
    public interface IImportFromRecipeService
    {
        public Task<IEnumerable<ShoppingItemSuggestion>> ImportFromOnlineRecipeAsync(string url, bool includeAppliances, string language);
    }

    public class ImportFromRecipeService : IImportFromRecipeService
    {
        private readonly HttpClient _httpClient;

        public ImportFromRecipeService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<IEnumerable<ShoppingItemSuggestion>> ImportFromOnlineRecipeAsync(string url, bool includeAppliances, string language)
        {
            var request = new
            {
                url,
                includeAppliances,
                language,
                categories = Enum.GetNames(typeof(Category))
            };

            var response = await _httpClient.PostAsJsonAsync("https://extractrecipeitems-tjbk5gsn3q-zf.a.run.app", request);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to extract recipe items: {response.StatusCode}");

            var result = await response.Content.ReadFromJsonAsync<Response>();

            if (result == null)
                return [];

            return result.Items.Select(i =>
            {
                var res = Enum.TryParse<Category>(i.Category, out var category);
                if (!res)
                    category = Category.Other;

                return new ShoppingItemSuggestion(
                    i.Name,
                    i.Quantity,
                    i.Unit,
                    category.GetDisplayWithIcon());
            });
        }

        private class Response
        {
            public ResponseItem[] Items { get; set; }
        }

        private class ResponseItem
        {
            public string Name { get; set; }
            public double? Quantity { get; set; }
            public string Unit { get; set; }
            public string Category { get; set; }
        }
    }
}
