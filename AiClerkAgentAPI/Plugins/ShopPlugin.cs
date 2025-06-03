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
            // Hole Produktliste wie gehabt
            var products = await _productService.GetProductsAsync();
            keywords = keywords.ToLower();

            var result = products.Where(p =>
                (!string.IsNullOrWhiteSpace(p.ProduktName) && p.ProduktName.ToLower().Contains(keywords))
                || (!string.IsNullOrWhiteSpace(p.Description) && p.Description.ToLower().Contains(keywords))
                || (p.Tags != null && p.Tags.Any(tag => tag.ToLower().Contains(keywords)))
            // Falls du Brand als Property hast, ergänze das hier:
            // || (!string.IsNullOrWhiteSpace(p.Brand) && p.Brand.ToLower().Contains(keywords))
            ).ToList();

            // Rückgabe: NUR Produkte aus dem Shop (keine Halluzinationen)
            return result;
        }
        [KernelFunction("suggest_products_by_description")]
        [Description("Suggests products whose description best matches the user's search keywords. Searches only in the product description and can find partial matches.")]
        public async Task<List<ProductModel>> SuggestProductsByDescriptionAsync([Description("Keywords or phrases to search for in the product description.")] string keywords)
        {
            var products = await _productService.GetProductsAsync();

            if (string.IsNullOrWhiteSpace(keywords))
                return new List<ProductModel>();

            var keywordsList = keywords
                .ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            var matchingProducts = products
                .Where(p => !string.IsNullOrEmpty(p.Description) &&
                            keywordsList.Any(kw => p.Description.ToLower().Contains(kw)))
                .ToList();

            return matchingProducts;
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
        [Description("Add a product with a specific name or keyword to the shopping cart. The function looks for a product matching the provided name and adds it to the user's current shopping cart session.")]
        public async Task<string> AddToCartByNameAsync([Description("The product name or any part of it that the user mentioned.")] string productName,
                                                       [Description("The user's current conversation ID.")] string conversationId)

             => await _cartService.AddToCartByNameAsync(productName, conversationId);

        [KernelFunction("remove_from_cart_by_name")]
        [Description("Remove a product with a specific name or keyword from the shopping cart. The function looks for a product matching the provided name in the user's current shopping cart session and removes it.")]
        public async Task<string> RemoveFromCartByNameAsync([Description("The product name or any part of it that the user mentioned.")] string productName,
                                                            [Description("The user's current conversation ID.")] string conversationId)

              => await _cartService.RemoveFromCartByNameAsync(productName, conversationId);

        [KernelFunction("get_cart")]
        [Description("Returns the current shopping cart for the given conversation ID.")]
        public async Task<CartModel?> GetCartAsync(string conversationId)

             => await Task.FromResult(_cartService.GetCart(conversationId));

        [KernelFunction("clear_cart")]
        [Description("Clears the entire shopping cart for the given conversation ID.")]
        public async Task ClearCartAsync(string conversationId)
        {
            _cartService.RemoveCart(conversationId);
            await Task.CompletedTask;
        }
    }
}