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
        public CartModel GetCart(string conversationId)
        {
            _cache.TryGetValue(conversationId, out CartModel? cart);
            return cart;
        }
        public void RemoveCart(string conversationId)
        {
            _cache.Remove(conversationId);
        }


        public async Task<string> AddToCartByNameAsync(string productName, string conversationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(conversationId))
                    return "Please specify both the product name and the conversation ID.";

                var products = await _productService.GetProductsAsync();
                var normalizedInput = productName.Trim().ToLowerInvariant();
                var matchedProduct = products.FirstOrDefault(p =>
                    !string.IsNullOrWhiteSpace(p.ProduktName) &&
                    p.ProduktName.ToLowerInvariant().Contains(normalizedInput));

                if (matchedProduct == null)
                    return $"🔍 I couldn't find any product with the name \"{productName}\".";

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

                return $"✅ The product \"{matchedProduct.ProduktName}\" has been added to your cart.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding to cart: {ex.Message}");
                return "❌ Internal error while adding to cart.";
            }
        }
        public async Task<string> RemoveFromCartByNameAsync(string productName, string conversationId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(productName) || string.IsNullOrWhiteSpace(conversationId))
                    return "Please specify both the product name and the conversation ID.";

                var cart = GetCart(conversationId);
                if (cart == null || cart.Items == null || !cart.Items.Any())
                    return "Your cart is empty or could not be accessed.";

                var normalizedInput = productName.Trim().ToLowerInvariant();
                var matchedItem = cart.Items.FirstOrDefault(i =>
                    !string.IsNullOrWhiteSpace(i.ProductName) &&
                    i.ProductName.ToLowerInvariant().Contains(normalizedInput)
                );

                if (matchedItem == null)
                    return $"🔍 I couldn't find any product with the name \"{productName}\" in your cart.";

                cart.Items.Remove(matchedItem);
                _cache.Set(conversationId, cart);

                return $"🗑️ The product *{matchedItem.ProductName}* has been removed from your cart.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error removing from cart: {ex.Message}");
                return "❌ Internal error while removing from cart.";
            }
        }
    }
}
