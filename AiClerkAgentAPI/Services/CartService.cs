using AiClerkAgentAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AiClerkAgentAPI.Services
{
    public class CartService
    {
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _cacheOptions = new() { SlidingExpiration = TimeSpan.FromHours(1)};

        public CartService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public CartModel GetorCreateCart(string conversationId)
        {
            if (!_cache.TryGetValue(conversationId, out CartModel cart))
            {
                cart = new CartModel() { ConversationId = conversationId };
                _cache.Set(conversationId, cart, _cacheOptions);
            }
            return cart;
        }

        public void AddToCart(string conversationId, CartItem item)
        {
            var cart = GetorCreateCart(conversationId);
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
            }
            else
            {
                cart.Items.Add(item);
            }
            _cache.Set(conversationId, cart, _cacheOptions);
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
