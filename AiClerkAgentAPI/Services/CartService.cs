using AiClerkAgentAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AiClerkAgentAPI.Services
{
    public class CartService
    {
        private readonly IMemoryCache _cache;
        private readonly ProductService _productService;

        public CartService(IMemoryCache cache, ProductService productService)
        {
            _cache = cache;
            _productService = productService;
        }

        public CartModel GetorCreateCart(string conversationId)
        {
            if (!_cache.TryGetValue(conversationId, out CartModel cart))
            {
                cart = new CartModel() { ConversationId = conversationId };
                _cache.Set(conversationId, cart);
            }
            return cart;
        }

        public async Task<string> AddToCartByNameAsync(string productName, string conversationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(conversationId))
                    return "❗ Bitte gib sowohl den Produktnamen als auch die Gesprächs-ID an.";

                var products = await _productService.GetProductsAsync();

                var normalizedInput = productName.Trim().ToLowerInvariant();
                var matchedProduct = products.FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.ProduktName) &&
                    p.ProduktName.ToLowerInvariant().Contains(normalizedInput));

                if (matchedProduct == null)
                    return $"🔍 Ich konnte kein Produkt finden mit dem Namen \"{productName}\".";

                var cartItem = new CartItem
                {
                    ProductId = matchedProduct.Id,
                    ProductName = matchedProduct.ProduktName,
                    Price = matchedProduct.Price,
                    Quantity = 1
                };

                var cart = GetorCreateCart(conversationId);
                var existing = cart.Items.FirstOrDefault(i => i.ProductId == cartItem.ProductId);

                if (existing != null)
                {
                    existing.Quantity += 1;
                }
                else
                {
                    cart.Items.Add(cartItem);
                }

                _cache.Set(conversationId, cart); 

                return $"✅ Das Produkt \"{matchedProduct.ProduktName}\" wurde erfolgreich in deinen Warenkorb gelegt.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Fehler beim Hinzufügen zum Warenkorb: {ex.Message}");
                return "❌ Interner Fehler beim Hinzufügen zum Warenkorb.";
            }
        }

        public CartModel GetCart(string conversationId)
        {
            _cache.TryGetValue(conversationId, out CartModel? cart);
            return cart;
        }

        public void RemoveCart(string conversationId)
        {
            _cache.Remove(conversationId);
        }
    }
}
