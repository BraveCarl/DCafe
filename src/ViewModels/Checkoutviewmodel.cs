using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStoreApp.Models;
using MauiStoreApp.Services;

namespace MauiStoreApp.ViewModels
{
    public partial class CheckoutViewModel : BaseViewModel
    {
        private readonly CartService _cartService;
        private readonly OrderService _orderService;
        private readonly FirebaseUserService _firebaseUserService;

        public CheckoutViewModel(CartService cartService, OrderService orderService, FirebaseUserService firebaseUserService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _firebaseUserService = firebaseUserService;
        }

        public CheckoutViewModel() { }

        // ── User inputs ───────────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPlaceOrder))]
        string fullName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPlaceOrder))]
        string phoneNumber;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPlaceOrder))]
        string street;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPlaceOrder))]
        string city;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPlaceOrder))]
        string province;

        // Computed single-line address for order submission
        public string FormattedAddress
        {
            get
            {
                var parts = new[] { Street, City, Province }
                    .Where(p => !string.IsNullOrWhiteSpace(p));
                return string.Join(", ", parts);
            }
        }

        // ── Payment Method ─────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCODSelected))]
        [NotifyPropertyChangedFor(nameof(IsGCashSelected))]
        [NotifyPropertyChangedFor(nameof(CanPlaceOrder))]
        string selectedPaymentMethod = "COD";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPlaceOrder))]
        string referenceNumber;

        public bool IsCODSelected => SelectedPaymentMethod == "COD";
        public bool IsGCashSelected => SelectedPaymentMethod == "GCash";

        [RelayCommand]
        void SelectCOD() => SelectedPaymentMethod = "COD";

        [RelayCommand]
        void SelectGCash() => SelectedPaymentMethod = "GCash";

        // ── QR Fullscreen ─────────────────────────────────────────────────────

        [ObservableProperty]
        bool isQRFullscreen;

        [RelayCommand]
        void ShowQRFullscreen() => IsQRFullscreen = true;

        [RelayCommand]
        void CloseQRFullscreen() => IsQRFullscreen = false;

        [ObservableProperty]
        bool isQRDownloaded;

        [RelayCommand]
        async Task DownloadQR()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("gcash_qr.png");
                var fileName = $"GCash_QR_{DateTime.Now:yyyyMMdd_HHmmss}.png";

#if ANDROID
                // Save to DCIM folder so it appears in Photos/Gallery
                var photosPath = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryDcim).AbsolutePath;
                var appFolder = Path.Combine(photosPath, "GCash");
                Directory.CreateDirectory(appFolder);
                var filePath = Path.Combine(appFolder, fileName);
#elif WINDOWS
                var photosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var filePath = Path.Combine(photosPath, fileName);
#else
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
#endif

                using var fileStream = File.Create(filePath);
                await stream.CopyToAsync(fileStream);

#if ANDROID
                // Notify gallery to scan the new file
                var context = Android.App.Application.Context;
                Android.Media.MediaScannerConnection.ScanFile(
                    context, new[] { filePath }, new[] { "image/png" }, null);
#endif

                IsQRDownloaded = true;
                await Shell.Current.DisplayAlert("Downloaded", "QR saved to Photos!", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CheckoutVM] DownloadQR error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Failed to download QR code.", "OK");
            }
        }

        [RelayCommand]
        async Task OpenPhotos()
        {
            try
            {
#if ANDROID
                var intent = new Android.Content.Intent();
                intent.SetAction(Android.Content.Intent.ActionView);
                intent.SetType("image/*");
                intent.SetFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
#elif WINDOWS
                var photosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(photosPath)
                });
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CheckoutVM] OpenPhotos error: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", "Could not open Photos app.", "OK");
            }
        }

        // ── FIX 2: OrderPlaced was missing entirely.
        //    CheckoutPage.xaml binds IsVisible of the form to:
        //      {Binding OrderPlaced, Converter={StaticResource InvertedBoolConverter}}
        //    or defaults to false → form is hidden → looks like cart is empty.
        [ObservableProperty]
        bool orderPlaced;

        // ── Cart summary ──────────────────────────────────────────────────────────

        public ObservableCollection<CartItemDetail> CartItems { get; } = new();

        public decimal TotalPrice => CartItems.Sum(i => i.Product.Price * i.Quantity);
        public string PaymentMethodDisplay => IsCODSelected ? "Cash on Delivery (COD)" : "GCash";

        // ── FIX 5: HasCartItems drives button VISIBILITY (separate from CanPlaceOrder).
        //    CanPlaceOrder drives button ENABLED state (requires all fields + items).
        //    This way the "Place Order" button appears as soon as there are cart items,
        //    even before the user fills in delivery details.
        public bool HasCartItems => CartItems.Count > 0;

        public bool CanPlaceOrder =>
            !string.IsNullOrWhiteSpace(FullName) &&
            !string.IsNullOrWhiteSpace(PhoneNumber) &&
            !string.IsNullOrWhiteSpace(Street) &&
            !string.IsNullOrWhiteSpace(City) &&
            !string.IsNullOrWhiteSpace(Province) &&
            CartItems.Count > 0 &&
            !IsBusy &&
            (IsCODSelected || !string.IsNullOrWhiteSpace(ReferenceNumber));

        // ── Init ──────────────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task Init()
        {
            CartItems.Clear();

            foreach (var item in _cartService.GetCartItems())
                CartItems.Add(item);

            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(CanPlaceOrder));
            // Notify HasCartItems so button visibility updates after load
            OnPropertyChanged(nameof(HasCartItems));

            // Auto-fill delivery details from Firebase profile
            await LoadUserDeliveryDetailsAsync();
        }

        // ── Auto-fill from Firebase ───────────────────────────────────────────────

        private async Task LoadUserDeliveryDetailsAsync()
        {
            try
            {
                var uid = await SecureStorage.GetAsync("firebaseUid");
                if (string.IsNullOrEmpty(uid)) return;

                var data = await _firebaseUserService.GetUserProfile(uid);
                if (data == null) return;

                FullName = SafeGet(data, "fullName");
                PhoneNumber = SafeGet(data, "phone");

                Street = SafeGet(data, "street");
                City = SafeGet(data, "city");
                Province = SafeGet(data, "province");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CheckoutVM] LoadUserDeliveryDetailsAsync: {ex.Message}");
            }
        }

        private static string SafeGet(Dictionary<string, object> data, string key)
        {
            if (data == null) return string.Empty;
            if (!data.TryGetValue(key, out var val)) return string.Empty;
            return val?.ToString() ?? string.Empty;
        }

        // ── Place Order ───────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task PlaceOrder()
        {
            if (IsBusy || !CanPlaceOrder) return;

            try
            {
                IsBusy = true;
                OnPropertyChanged(nameof(CanPlaceOrder));

                var (success, order, error) =
                    await _orderService.PlaceOrderAsync(
                        FullName, PhoneNumber, FormattedAddress,
                        SelectedPaymentMethod, ReferenceNumber);

                if (!success)
                {
                    await Shell.Current.DisplayAlert("Cannot place order", error, "OK");
                    return;
                }

                CartItems.Clear();
                OnPropertyChanged(nameof(TotalPrice));
                OnPropertyChanged(nameof(HasCartItems));

                Debug.WriteLine($"[Checkout] Order #{order.OrderId} — ₱{order.TotalAmount:F2}");

                await Shell.Current.GoToAsync($"SuccessPage?OrderId={order.OrderId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Checkout] PlaceOrder error: {ex.Message}");
                await Shell.Current.DisplayAlert(
                    "Error", "Failed to place your order. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(CanPlaceOrder));
            }
        }

        [RelayCommand]
        public async Task BackToHome()
            => await Shell.Current.GoToAsync("//HomePage");
    }
}