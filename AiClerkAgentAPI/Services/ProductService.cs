using AiClerkAgentAPI.Models;
using System.Text.Json;

namespace AiClerkAgentAPI.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private List<ProductModel> _products;



        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _products = LoadProductsAsync().GetAwaiter().GetResult();
        }

        public async Task<List<ProductModel>> LoadProductsAsync()
        {
            var response = await _httpClient.GetAsync("https://dummyjson.com/products");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var productsJson = doc.RootElement.GetProperty("products").GetRawText();

            var products = JsonSerializer.Deserialize<List<ProductModel>>(productsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return products ?? new List<ProductModel>();
        }
        public Task<List<ProductModel>> GetProductsAsync()
        { 
            return  Task.FromResult(_products);
   
        }
}
}
