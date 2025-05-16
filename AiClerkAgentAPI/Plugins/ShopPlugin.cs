using AiClerkAgentAPI.Models;
using AiClerkAgentAPI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Globalization;

namespace AiClerkAgentAPI.Plugins
{
    public class ShopPlugin
    {
        private readonly ProductService _productService;
        private readonly CartService _cartService;
        private readonly IMemoryCache _cache;

        public ShopPlugin(ProductService productService)
        {
            _productService = productService;
        }

        [KernelFunction("get_products_by_category")]
        [Description("Holt eine Liste von Produkten basierend auf einer angegebenen Kategorie.")]
        public async Task<List<ProductModel>> GetProductsByCategoryAsync([Description("category von der userinput")] string category)
        {
            var products = await _productService.GetProductsAsync();

            var matchingProducts = products
                .Where(p => !string.IsNullOrWhiteSpace(p.Category) &&
                            p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return matchingProducts;
        }

        [KernelFunction("suggest_products")]
        [Description("Schlägt dem Nutzer passende Produkte vor basierend auf den angegebenen Schlüsselwörtern.")]
        public async Task<List<ProductModel>> SuggestProductsAsync(string keywords)
        {
            var products = _productService.GetProductsAsync().Result;
            keywords = keywords.ToLower();

            var result = products.Where(p =>
                (!string.IsNullOrWhiteSpace(p.ProduktName) && p.ProduktName.ToLower().Contains(keywords)) ||
                (!string.IsNullOrWhiteSpace(p.Category) && p.Category.ToLower().Contains(keywords))
            ).ToList();

            return await Task.FromResult(result);
        }

        [KernelFunction("get_categories")]
        [Description("Gibt eine Liste aller Produktkategorien im Onlineshop zurück.")]
        public async Task<List<string>> GetCategoriesAsync()
        {
            var products = _productService.GetProductsAsync().Result;

            var categories = products
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return await Task.FromResult(categories);
        }
        [KernelFunction("get_all_products")]
        [Description("Gibt alle verfügbaren Produkte zurück, damit die KI selbst die passenden Produkte für das vom Nutzer genannte Event oder Szenario auswählen kann." +
                     " Die KI kann erkennen, ob es sich z.B. um ein Date, eine Hochzeit, ein Bewerbungsgespräch, ein Geschenk, " +
                     "einen Urlaub oder andere Anlässe handelt und die relevantesten Produkte für diesen spezifischen Kontext empfehlen.")]
        public async Task<List<ProductModel>> GetAllProductsAsync()
        {
            var products = _productService.GetProductsAsync().Result;
            return await Task.FromResult(products);
        }
        [KernelFunction("get_new_products")]
        [Description("Gibt die neuesten Produkte zurück, sortiert nach Erstellungsdatum. " +
                    "Optional: Anzahl der zurückzugebenden Elemente (default 3).")]
        public async Task<List<ProductModel>> GetNewProductsAsync([Description("Maximale Anzahl an neuesten Produkten")] int topN = 3)
        {
            var products = await _productService.GetProductsAsync();
            var parsed = products
                .Select(p =>
                {
                    if (p.Meta != null &&
                        DateTime.TryParse(p.Meta.createdAt,
                                          null,
                                          DateTimeStyles.RoundtripKind,
                                          out var dt))
                    {
                        return new { Product = p, Date = dt };
                    }
                    else
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .Cast<dynamic>();

            var newest = parsed
                .OrderByDescending(x => x.Date)
                .Take(topN)
                .Select(x => (ProductModel)x.Product!)
                .ToList();

            return newest;
        }
        [KernelFunction("add_last_suggested_to_cart")]
        [Description("Fügt das zuletzt vorgeschlagene Produkt in den Warenkorb ein.")]
        public Task<CartModel> AddLastSuggestedToCartAsync([Description("ConversationId des Nutzers")] string conversationId,
                                                           [Description("Anzahl (optional, default = 1)")] int quantity = 1)
        { 

            var item = new CartItem
            {
                ProductId = last.Id,
                ProductName = last.ProduktName,
                Price = last.Price,
                Quantity = quantity
            };

            _cartService.AddToCart(conversationId, item);
            return Task.FromResult(_cartService.GetCart(conversationId)!);
        }
        [KernelFunction("get_cart")]
        [Description("Gibt den aktuellen Warenkorb zurück.")]
        public Task<CartModel?> GetCartAsync(string conversationId)
        {
            var cart = _cartService.GetCart(conversationId);
            return Task.FromResult(cart);
        }
        [KernelFunction("clear_cart")]
        [Description("Löscht den kompletten Warenkorb.")]
        public Task ClearCartAsync(string conversationId)
        {
            _cartService.RemoveCart(conversationId);
            return Task.CompletedTask;
        }
    }
}