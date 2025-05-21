using AiClerkAgentAPI.Models;
using AiClerkAgentAPI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AiClerkAgentAPI.Plugins
{
    public class ShopPlugin
    {
        private readonly ProductService _productService;
        private readonly CartService _cartService;
        public ShopPlugin(ProductService productService, CartService cartService)
        {
            _productService = productService;
            _cartService = cartService;
        }

        [KernelFunction("get_products_by_category")]
        [Description("Returns a list of products based on a given category.")]
        public async Task<List<ProductModel>> GetProductsByCategoryAsync([Description("The category provided by the user input.")] string category)
        {
            var products = await _productService.GetProductsAsync();

            var matchingProducts = products
                .Where(p => !string.IsNullOrWhiteSpace(p.Category) &&
                            p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return matchingProducts;
        }

        [KernelFunction("suggest_products")]
        [Description("Suggest suitable products to the user based on the given keywords.")]
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
        [Description("Returns a list of all product categories available in the online shop.")]
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
        [Description("Returns the newest products, sorted by creation date. Optional: Number of products to return (default: 3).")]
        public async Task<List<ProductModel>> GetNewProductsAsync([Description("Maximum number of newest products to return.")] int topN = 3)
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
        [Description("Add a product with a specific name or keyword to the shopping cart. The function looks for a product matching " +
                     "the provided name and adds it to the user's current shopping cart session.")]
        public async Task<string> AddToCartByNameAsync([Description("The product name or any part of it that the user mentioned.")] string productName,
                                                       [Description("The user's current conversation ID.")] string conversationId)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return "Please specify the product name you want to add.";

            if (string.IsNullOrWhiteSpace(conversationId))
                return "A valid conversation ID is missing. Please start a new conversation.";

            // Produkte laden
            var products = await _productService.GetProductsAsync();
            var normalizedInput = productName.Trim().ToLowerInvariant();

            var matchedProduct = products
                        .FirstOrDefault(p =>  !string.IsNullOrWhiteSpace(p.ProduktName) &&
                                                p.ProduktName.ToLowerInvariant().Contains(normalizedInput)
    );


            if (matchedProduct == null)
                return $"🔍 I couldn't find any product with the name \"{productName}\". Please try rephrasing it.";

            var cartItem = new CartItem
            {
                ProductId = matchedProduct.Id,
                ProductName = matchedProduct.ProduktName,
                Price = matchedProduct.Price,
                Quantity = 1
            };

            var cart = _cartService?.GetorCreateCart(conversationId);
            if (cart == null)
                return "❌ There was a problem accessing your shopping cart. Please try again.";

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == cartItem.ProductId);
            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                cart.Items.Add(cartItem);
            }

            return $"✅ The product **{matchedProduct.ProduktName}** has been added to your cart.";
        }
        [KernelFunction("remove_from_cart_by_name")]
        [Description("Remove a product with a specific name or keyword from the shopping cart. The function looks for a product matching the provided name in the user's current shopping cart session and removes it.")]
        public async Task<string> RemoveFromCartByNameAsync(
        [Description("The product name or any part of it that the user mentioned.")] string productName,
        [Description("The user's current conversation ID.")] string conversationId)
        {
            if (string.IsNullOrWhiteSpace(productName))
                return "Please specify the product name you want to remove.";

            if (string.IsNullOrWhiteSpace(conversationId))
                return "A valid conversation ID is missing. Please start a new conversation.";

            var cart = _cartService?.GetCart(conversationId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                return "Your cart is empty or could not be accessed.";

            var normalizedInput = productName.Trim().ToLowerInvariant();
            var matchedItem = cart.Items.FirstOrDefault(i =>
                !string.IsNullOrWhiteSpace(i.ProductName) &&
                i.ProductName.ToLowerInvariant().Contains(normalizedInput)
            );

            if (matchedItem == null)
                return $"🔍 I couldn't find any product with the name \"{productName}\" in your cart. Please try rephrasing it.";

            cart.Items.Remove(matchedItem);
            return $"🗑️ The product *{matchedItem.ProductName}* has been removed from your cart.";
        }

        [KernelFunction("get_cart")]
        [Description("Returns the current shopping cart for the given conversation ID.")]
        public async Task<CartModel?> GetCartAsync(string conversationId)
        {
            var cart = _cartService.GetCart(conversationId);
            return await Task.FromResult(cart);
        }
        [KernelFunction("clear_cart")]
        [Description("Clears the entire shopping cart for the given conversation ID.")]
        public async Task ClearCartAsync(string conversationId)
        {
            _cartService.RemoveCart(conversationId);
            await Task.CompletedTask;
        }
    }
}