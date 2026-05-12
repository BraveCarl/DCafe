using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCafe.Models;
using DCafe.Services;

namespace DCafe.ViewModels
{
    public partial class CartViewModel : BaseViewModel
    {
        private readonly CartService _cartService;
        private readonly AuthService _authService;

        public CartViewModel(CartService cartService, AuthService authService)
        {
            _cartService = cartService;
            _authService = authService;
        }

        public CartViewModel() { }

        [ObservableProperty]
        public bool isUserLoggedIn;

        [ObservableProperty]
        private bool isBusyWithCartModification;

        public ObservableCollection<CartItemDetail> CartItems { get; private set; }
            = new ObservableCollection<CartItemDetail>();

        // ── FIX: IsUserLoggedIn re-evaluated on EVERY navigation ─────────────────
        // The original code used isFirstRun — after login and navigating back,
        // isFirstRun was false so Init() called SyncCartItems() instead of
        // re-checking auth. IsUserLoggedIn stayed false, Sign In stayed visible.
        [RelayCommand]
        public async Task Init()
        {
            // Always re-resolve login state — this is what fixes the "still shows
            // Sign In after login" bug. IsUserLoggedIn now checks both "token" and
            // "firebaseUid" via the fixed AuthService.
            IsUserLoggedIn = _authService.IsUserLoggedIn;

            if (IsUserLoggedIn)
            {
                // Sync in-memory items first (picks up pending-cart additions)
                SyncCartItems();

                // If cart is still empty, try loading from server (integer userId only)
                if (CartItems.Count == 0)
                    await TryLoadServerCartAsync();
            }
        }

        // ── Server cart load (integer userId only — skips Firebase UID) ──────────
        private async Task TryLoadServerCartAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var userIdStr = await SecureStorage.GetAsync("userId");

                // Firebase UIDs are strings like "Xk29aZ…" — int.TryParse returns false.
                // Gracefully skip the fake-store API call for Firebase users.
                if (!int.TryParse(userIdStr, out int userId))
                {
                    Debug.WriteLine($"[CartVM] Skipping server cart — '{userIdStr}' is a Firebase UID.");
                    return;
                }

                await _cartService.RefreshCartItemsByUserIdAsync(userId);
                SyncCartItems();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CartVM] TryLoadServerCart: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to retrieve cart.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task GoToLoginPage()
            => await Shell.Current.GoToAsync("LoginPage");

        [RelayCommand]
        private async Task GoToCheckout()
            => await Shell.Current.GoToAsync("CheckoutPage");

        [RelayCommand]
        public async Task DeleteCart()
        {
            if (IsBusy || IsBusyWithCartModification) return;

            try
            {
                var confirmed = await Shell.Current.DisplayAlert(
                    "Confirm", "Are you sure you want to delete the cart?", "Yes", "No");
                if (!confirmed) return;

                if (CartItems.Count == 0)
                {
                    await Shell.Current.DisplayAlert("Error", "No cart found.", "OK");
                    return;
                }

                IsBusyWithCartModification = true;

                var response = await _cartService.DeleteCartAsync();
                if (response?.IsSuccessStatusCode == true)
                {
                    CartItems.Clear();
                    await Toast.Make("Cart deleted successfully.", ToastDuration.Short)
                               .Show(new CancellationTokenSource().Token);
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to delete cart.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteCart: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to delete cart.", "OK");
            }
            finally
            {
                IsBusyWithCartModification = false;
            }
        }

        [RelayCommand]
        public void IncreaseProductQuantity(Product product)
        {
            try { _cartService.IncreaseProductQuantity(product.Id); SyncCartItems(); }
            catch (Exception ex)
            {
                Debug.WriteLine($"IncreaseQty: {ex.Message}");
                Shell.Current.DisplayAlert("Error", "Failed to increase quantity.", "OK");
            }
        }

        [RelayCommand]
        public void DecreaseProductQuantity(Product product)
        {
            try { _cartService.DecreaseProductQuantity(product.Id); SyncCartItems(); }
            catch (Exception ex)
            {
                Debug.WriteLine($"DecreaseQty: {ex.Message}");
                Shell.Current.DisplayAlert("Error", "Failed to decrease quantity.", "OK");
            }
        }

        // FIX: expose HasCartItems so CartPage.xaml can bind button visibility
        public bool HasCartItems => CartItems.Count > 0;

        private void SyncCartItems()
        {
            var source = _cartService.GetCartItems();

            foreach (var item in CartItems.ToList())
                if (!source.Any(s => s.Product.Id == item.Product.Id))
                    CartItems.Remove(item);

            foreach (var updated in source)
            {
                var existing = CartItems.FirstOrDefault(c => c.Product.Id == updated.Product.Id);
                if (existing == null)
                    CartItems.Add(updated);
                else
                    existing.Quantity = updated.Quantity;
            }

            OnPropertyChanged(nameof(HasCartItems));
        }
    }
}