using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCafe.Services;
using DCafe.Views;
using System.Diagnostics;

namespace DCafe.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly FirebaseAuthService _firebaseAuth;
        private readonly PendingCartService _pendingCartService;
        private readonly CartService _cartService;
        private readonly AuthService _authService;
        // ADD alongside the existing four fields
        private readonly FirebaseUserService _firebaseUserService;

        public LoginViewModel(
            FirebaseAuthService firebaseAuth,
            PendingCartService pendingCartService,
            CartService cartService,
            AuthService authService,
            FirebaseUserService firebaseUserService)
        {
            _firebaseAuth = firebaseAuth;
            _pendingCartService = pendingCartService;
            _cartService = cartService;
            _authService = authService;
            _firebaseUserService = firebaseUserService;
        }

        public LoginViewModel() { }

        [ObservableProperty] string username;
        [ObservableProperty] string password;
        [ObservableProperty] bool isPasswordVisible;

        [RelayCommand]
        public async Task Login()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert(
                    "Missing fields", "Please enter your email and password.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                var (success, error) = await _firebaseAuth.LoginAsync(Username.Trim(), Password);

                if (!success)
                {
                    await Shell.Current.DisplayAlert("Login failed", error, "OK");
                    return;
                }

                var uid = await SecureStorage.GetAsync("firebaseUid");
                _authService.NotifyFirebaseLogin(uid);
                Debug.WriteLine(
                    $"Firebase login OK — uid: {await SecureStorage.Default.GetAsync("firebaseUid")}");


                await HandleRoleNavigationAsync(uid);   // ← checks role FIRST, then delegates
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert(
                    "Error", "Something went wrong. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task HandleRoleNavigationAsync(string uid)
        {
            var role = await _firebaseUserService.GetUserRoleAsync(uid);

            if (role == "courier")
            {
                await Shell.Current.GoToAsync(nameof(CourierDashboardPage));
            }
            else
            {
                await HandlePendingCartAsync();   // existing method — completely untouched
            }
        }

        [RelayCommand]
        public void TogglePasswordVisibility()
            => IsPasswordVisible = !IsPasswordVisible;

        [RelayCommand]
        private async Task GoToRegister()
            => await Shell.Current.GoToAsync("RegisterPage");

        // ── Pending-cart handler ──────────────────────────────────────────────────
        // Called right after every successful login.
        //
        // If the user tapped "Add to Cart" while logged out, ProductDetailsViewModel
        // called PendingCartService.SavePendingProduct(product) before redirecting
        // here.  ConsumePendingProduct() returns that product ONCE and clears it so
        // it can never be double-added.
        //
        // Flow A — pending product exists:
        //   1. Add it to CartService (singleton, shared with CartViewModel).
        //   2. Show a confirmation alert.
        //   3. Navigate to CartPage  →  CartViewModel.Init() runs, sees the item
        //      already in CartService, and shows it immediately via SyncCartItems().
        //
        // Flow B — normal login (no pending product):
        //   Navigate back or to HomePage as before.
        private async Task HandlePendingCartAsync()
        {
            var pending = _pendingCartService.ConsumePendingProduct();

            if (pending != null)
            {
                // Add to the singleton CartService so CartViewModel can read it
                _cartService.AddProductToCart(pending);

                await Shell.Current.DisplayAlert(
                    "Added to cart",
                    $"\"{pending.Title}\" has been added to your cart.",
                    "View Cart");

                // CartViewModel.Init() will call SyncCartItems() and show it
                await Shell.Current.GoToAsync("//CartPage");
            }
            else
            {
                var navStack = Shell.Current.Navigation.NavigationStack;
                if (navStack.Count >= 2)
                    await Shell.Current.Navigation.PopAsync();
                else
                    await Shell.Current.GoToAsync("//HomePage");
            }
        }
    }
}