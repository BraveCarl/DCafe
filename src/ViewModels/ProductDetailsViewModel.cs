using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCafe.Models;
using DCafe.Services;
using DCafe.Views;

namespace DCafe.ViewModels
{
    [QueryProperty(nameof(Product), "Product")]
    public partial class ProductDetailsViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private readonly AuthService _authService;
        private readonly CartService _cartService;
        private readonly RecentlyViewedProductsService _recentlyViewedProductsService;
        private readonly PendingCartService _pendingCartService;

        public ProductDetailsViewModel(
            ProductService productService,
            CartService cartService,
            RecentlyViewedProductsService recentlyViewedProductsService,
            AuthService authService,
            PendingCartService pendingCartService)
        {
            _productService = productService;
            _cartService = cartService;
            _recentlyViewedProductsService = recentlyViewedProductsService;
            _authService = authService;
            _pendingCartService = pendingCartService;
        }

        public ProductDetailsViewModel() { }

        // ── Properties ────────────────────────────────────────────────────────────

        [ObservableProperty]
        Product product;

        public ObservableCollection<Product> CrossSellProducts { get; private set; }
            = new ObservableCollection<Product>();

        // ── Commands ──────────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task Init()
            => await GetCrossSellProductsAsync();

        // FIX 3: GoBackCommand was missing entirely.
        // ProductDetailsPage.xaml line 47 binds the back button to GoBackCommand
        // via RelativeSource. Without this command the button silently did nothing.
        [RelayCommand]
        private async Task GoBackAsync()
            => await Shell.Current.Navigation.PopAsync();

        [RelayCommand]
        private async Task ProductTapped(Product product)
        {
            if (product == null) return;

            IsBusy = true;
            _recentlyViewedProductsService.AddProduct(product);
            await Shell.Current.GoToAsync(
                nameof(ProductDetailsPage), true,
                new Dictionary<string, object> { { "Product", product } });
            IsBusy = false;
        }

        [RelayCommand]
        private async Task ShareProduct(Product product)
        {
            if (product == null) return;

            await Share.RequestAsync(new ShareTextRequest
            {
                Uri = product.Image,
                Title = product.Title,
                Text = "Hey, check out this product I found on DCafé!",
            });
        }

        [RelayCommand]
        private async Task AddToCart(Product product)
        {
            if (IsBusy || product == null) return;

            try
            {
                IsBusy = true;

                // ↓ FIXED: use AuthService instead of raw SecureStorage reads
                if (!_authService.IsUserLoggedIn)
                {
                    _pendingCartService.SavePendingProduct(product);
                    await Shell.Current.GoToAsync(nameof(LoginPage));
                    return;
                }

                await AddProductAndNotifyAsync(product);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddToCart error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to add product to cart.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        internal async Task AddProductAndNotifyAsync(Product product)
        {
            _cartService.AddProductToCart(product);

            bool goToCart = await Shell.Current.DisplayAlert(
                "DCafé ☕",
                $"{product.Title} added to cart.",
                "View Cart",
                "Continue Shopping");

            if (goToCart)
                await Shell.Current.GoToAsync("CartPage");
        }

        private async Task GetCrossSellProductsAsync()
        {
            if (IsBusy || Product == null) return;

            try
            {
                IsBusy = true;
                var products = await _productService.GetProductsByCategoryAsync(Product.Category);
                CrossSellProducts.Clear();
                foreach (var p in products)
                    if (p.Id != Product.Id)
                        CrossSellProducts.Add(p);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CrossSell error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Unable to get cross-sell products.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}