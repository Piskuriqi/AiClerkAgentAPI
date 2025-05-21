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

        public ShopPlugin(ProductService productService, CartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
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
        [KernelFunction("add_to_cart_by_name")]
        [Description("Fügt ein Produkt mit einem bestimmten Namen oder Schlüsselwort in den Warenkorb ein.")]
        public async Task<string> AddToCartByNameAsync(
        [Description("Der Produktname oder ein Teil davon, den der Nutzer erwähnt hat")] string productName,
        [Description("Die aktuelle Conversation ID des Nutzers")] string conversationId)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return "Bitte gib den Produktnamen an, den du hinzufügen möchtest.";

            if (string.IsNullOrWhiteSpace(conversationId))
                return "Es fehlt eine gültige Konversations-ID. Bitte starte eine neue Unterhaltung.";

            // Produkte laden
            var products = await _productService.GetProductsAsync();
            var normalizedInput = productName.Trim().ToLowerInvariant();

            var matchedProduct = products
                        .FirstOrDefault(p =>  !string.IsNullOrWhiteSpace(p.ProduktName) &&
                                                p.ProduktName.ToLowerInvariant().Contains(normalizedInput)
    );


            if (matchedProduct == null)
                return $"🔍 Ich konnte kein Produkt mit dem Namen \"{productName}\" finden. Bitte formuliere es eventuell etwas anders.";

            // CartItem erzeugen
            var cartItem = new CartItem
            {
                ProductId = matchedProduct.Id,
                ProductName = matchedProduct.ProduktName,
                Price = matchedProduct.Price,
                Quantity = 1
            };

            // Warenkorb holen oder anlegen
            var cart = _cartService?.GetorCreateCart(conversationId);
            if (cart == null)
                return "❌ Es gab ein Problem beim Zugriff auf deinen Warenkorb. Bitte versuche es erneut.";

            // Produkt hinzufügen oder erhöhen
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == cartItem.ProductId);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                cart.Items.Add(cartItem);
            }

            return $"✅ Das Produkt **{matchedProduct.ProduktName}** wurde deinem Warenkorb hinzugefügt.";
        }

        [KernelFunction("get_cart")]
        [Description("Gibt den aktuellen Warenkorb zurück.")]
        public async Task<CartModel?> GetCartAsync(string conversationId)
        {
            var cart = _cartService.GetCart(conversationId);
            return await Task.FromResult(cart);
        }
        [KernelFunction("clear_cart")]
        [Description("Löscht den kompletten Warenkorb.")]
        public async Task ClearCartAsync(string conversationId)
        {
            _cartService.RemoveCart(conversationId);
            await Task.CompletedTask;
        }
    }
}