using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStoreApp.Models;
using MauiStoreApp.Services;
using MauiStoreApp.Views;

namespace MauiStoreApp.ViewModels
{
    public partial class HomePageViewModel : BaseViewModel
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly RecentlyViewedProductsService _recentlyViewedProductsService;

        // ── Source collections (never modified after load) ────────────────────────
        public ObservableCollection<Product> Products { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();

        // ── FIX: FilteredProducts is what the CollectionView binds to.
        //    It starts as a copy of Products and is rebuilt on every keystroke.
        //    Using a separate collection means Products is never mutated —
        //    no risk of losing data if the user clears the search.
        public ObservableCollection<Product> FilteredProducts { get; } = new();

        // ── FIX: SearchText drives real-time filtering.
        //    The partial void OnSearchTextChanged hook fires automatically
        //    whenever the bound Entry changes — no command or button needed.
        [ObservableProperty]
        string searchText;

        partial void OnSearchTextChanged(string value) => ApplyFilter(value);

        private bool isFirstRun = true;

        public HomePageViewModel(
            ProductService productService,
            CategoryService categoryService,
            RecentlyViewedProductsService recentlyViewedProductsService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _recentlyViewedProductsService = recentlyViewedProductsService;
        }

        public HomePageViewModel() { }

        // ── Init ──────────────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task Init()
        {
            if (!isFirstRun) return;

            try
            {
                Debug.WriteLine("[HomePageViewModel] Init starting...");
                await GetProductsAsync();
                Debug.WriteLine("[HomePageViewModel] Products loaded");
                await GetCategoriesAsync();
                Debug.WriteLine("[HomePageViewModel] Categories loaded");

                try
                {
                    _recentlyViewedProductsService?.LoadProducts();
                    Debug.WriteLine("[HomePageViewModel] Recently viewed loaded");
                }
                catch (Exception rvEx)
                {
                    Debug.WriteLine($"[HomePageViewModel] RecentlyViewed error: {rvEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CRASH] Init failed: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                isFirstRun = false;
                Debug.WriteLine("[HomePageViewModel] Init completed");
            }
        }

        // ── Filter ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds FilteredProducts from the master Products list.
        /// Called instantly on every keystroke via OnSearchTextChanged.
        /// No Task.Run needed — LINQ on an in-memory list is non-blocking.
        /// </summary>
        private void ApplyFilter(string query)
        {
            FilteredProducts.Clear();

            // Empty/null query → show everything
            var source = string.IsNullOrWhiteSpace(query)
                ? Products
                : Products.Where(p =>
                    p?.Title?.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) == true);

            foreach (var product in source)
                FilteredProducts.Add(product);
        }

        // ── Data loading ──────────────────────────────────────────────────────────

        private async Task GetCategoriesAsync()
        {
            try
            {
                var categories = await _categoryService.GetCategoriesAsync();
                if (categories == null) return;

                Categories.Clear();
                foreach (var category in categories)
                    Categories.Add(category);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get categories: {ex.Message}");
            }
        }

        private async Task GetProductsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var products = await _productService.GetProductsAsync();
                if (products == null) return;

                var list = products.ToList();

                Products.Clear();
                foreach (var product in list)
                    Products.Add(product);

                // Populate FilteredProducts immediately after load
                // so the grid shows everything before the user types anything.
                ApplyFilter(SearchText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get products: {ex.Message}");
                try { await Shell.Current.DisplayAlert("Error!", ex.Message, "OK"); } catch { }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Navigation commands ───────────────────────────────────────────────────

        [RelayCommand]
        private async Task ProductTapped(Product product)
        {
            if (product == null) return;

            _recentlyViewedProductsService.AddProduct(product);

            await Shell.Current.GoToAsync(
                $"{nameof(ProductDetailsPage)}", true,
                new Dictionary<string, object> { { "Product", product } });
        }

        [RelayCommand]
        private async Task CategoryTapped(Category category)
        {
            if (category == null) return;

            await Shell.Current.GoToAsync(
                $"{nameof(CategoryPage)}", true,
                new Dictionary<string, object> { { "Category", category } });
        }
    }
}