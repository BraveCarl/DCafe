using MauiStoreApp.Models;

namespace MauiStoreApp.Services
{
    /// <summary>
    /// Temporarily holds a product that a guest user tried to add to cart
    /// before being redirected to the login page.
    ///
    /// Flow:
    ///   1. User taps "Add to Cart" while not logged in.
    ///   2. ProductDetailsViewModel calls SavePendingProduct(product).
    ///   3. User is sent to LoginPage.
    ///   4. After a successful login, LoginViewModel calls
    ///      ConsumePendingProduct() — which returns the product once
    ///      and then clears it so it is never double-added.
    /// </summary>
    public class PendingCartService
    {
        private Product _pendingProduct;

        /// <summary>True when there is a product waiting to be added after login.</summary>
        public bool HasPendingProduct => _pendingProduct != null;

        /// <summary>
        /// Saves the product the guest tried to add so it can be
        /// retrieved after a successful login.
        /// </summary>
        public void SavePendingProduct(Product product)
            => _pendingProduct = product;

        /// <summary>
        /// Returns the saved product and clears it in one atomic step.
        /// Returns null if there is no pending product.
        /// </summary>
        public Product ConsumePendingProduct()
        {
            var product = _pendingProduct;
            _pendingProduct = null;
            return product;
        }
    }
}