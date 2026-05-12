using System.Text.Json;
using MauiStoreApp.Models;

namespace MauiStoreApp.Services
{
    /// <summary>
    /// Provides services for managing and retrieving products.
    /// </summary>
    public class ProductService : BaseService
    {
        /// <summary>
        /// Asynchronously retrieves a collection of all available products, optionally sorted in a specific order.
        /// </summary>
        /// <param name="sortOrder">The order in which the products should be sorted. Defaults to "asc" for ascending.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains an enumerable collection of <see cref="Product"/> objects.
        /// </returns>
        public async Task<IEnumerable<Product>> GetProductsAsync(string sortOrder = "asc")
        {
            var json = await GetRawJsonAsync("products.json");
            return ParseProducts(json);
        }

        /// <summary>
        /// Asynchronously retrieves a product by its ID.
        /// </summary>
        /// <param name="id">The ID of the product to retrieve.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains the <see cref="Product"/> object with the specified ID.
        /// </returns>
        public async Task<Product> GetProductByIdAsync(int id)
        {
            // Try direct key lookup first (works when products are stored keyed by ID).
            var single = await GetAsync<Product>($"products/{id}.json");
            if (single != null && single.Id == id)
                return single;

            // Fallback: fetch all and find by ID (handles array storage).
            var all = await GetProductsAsync();
            return all?.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Asynchronously retrieves a collection of products belonging to a specific category, optionally sorted in a specific order.
        /// </summary>
        /// <param name="category">The category of the products to retrieve.</param>
        /// <param name="sortOrder">The order in which the products should be sorted. Defaults to "asc" for ascending.</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains an enumerable collection of <see cref="Product"/> objects belonging to the specified category.
        /// </returns>
        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(string category, string sortOrder = "asc")
        {
            var allProducts = await GetProductsAsync();
            if (allProducts == null)
                return Enumerable.Empty<Product>();

            var filtered = allProducts.Where(p => string.Equals(p?.Category, category, StringComparison.OrdinalIgnoreCase));

            // Optionally apply simple sorting by price
            if (sortOrder?.ToLowerInvariant() == "desc")
                filtered = filtered.OrderByDescending(p => p?.Price);
            else
                filtered = filtered.OrderBy(p => p?.Price);

            return filtered;
        }

        /// <summary>
        /// Parses products from a raw Firebase JSON response.
        /// Handles three formats Firebase might return:
        ///   1. JSON array:    [{ ... }, { ... }, ...]
        ///   2. Keyed object:  { "1": { ... }, "2": { ... }, ... }
        ///   3. Wrapped array: { "products": [{ ... }, ...] }  (imported with wrapper)
        /// </summary>
        private static IEnumerable<Product> ParseProducts(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
                return Enumerable.Empty<Product>();

            // Strategy 1: JSON array
            try
            {
                var list = JsonSerializer.Deserialize<List<Product>>(json);
                if (list != null)
                    return list.Where(p => p != null);
            }
            catch { /* not an array — try next */ }

            // Strategy 2: Keyed object { "1": {...}, "2": {...} }
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, Product>>(json);
                if (dict != null)
                    return dict.Values.Where(p => p != null);
            }
            catch { /* not a simple keyed object — try next */ }

            // Strategy 3: Wrapped { "products": [...] } or { "coffee": [...] } or any key containing array
            try
            {
                var wrapper = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (wrapper != null)
                {
                    // Try known keys first: "products", "coffee"
                    string[] knownKeys = { "products", "coffee" };
                    foreach (var key in knownKeys)
                    {
                        if (wrapper.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Array)
                        {
                            var products = JsonSerializer.Deserialize<List<Product>>(element.GetRawText());
                            if (products != null && products.Count > 0)
                                return products.Where(p => p != null);
                        }
                    }
                    
                    // Fallback: try first array value in wrapper
                    foreach (var kvp in wrapper)
                    {
                        if (kvp.Value.ValueKind == JsonValueKind.Array)
                        {
                            var products = JsonSerializer.Deserialize<List<Product>>(kvp.Value.GetRawText());
                            if (products != null && products.Count > 0)
                                return products.Where(p => p != null);
                        }
                    }
                }
            }
            catch { /* nothing worked */ }

            return Enumerable.Empty<Product>();
        }
    }
}

