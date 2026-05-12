using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCafe.Models;
using DCafe.Services;

namespace DCafe.ViewModels
{
    /// <summary>
    /// Backs CourierDashboardPage.
    /// Two tabs:
    ///   AvailableOrders — Pending orders with no courier yet (courier can accept)
    ///   MyOrders        — Orders this courier has accepted (courier advances status)
    /// </summary>
    public partial class CourierDashboardViewModel : BaseViewModel
    {
        private readonly CourierService _courierService;
        private readonly FirebaseAuthService _auth;

        public CourierDashboardViewModel(CourierService courierService, FirebaseAuthService auth)
        {
            _courierService = courierService;
            _auth = auth;
        }

        public CourierDashboardViewModel() { }

        // ── Collections ───────────────────────────────────────────────────────────

        public ObservableCollection<Order> AvailableOrders { get; } = new();
        public ObservableCollection<Order> MyOrders { get; } = new();

        // ── State ─────────────────────────────────────────────────────────────────

        [ObservableProperty] bool hasNoAvailable;
        [ObservableProperty] bool hasNoMyOrders;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAvailableTab))]
        [NotifyPropertyChangedFor(nameof(IsMyOrdersTab))]
        int selectedTabIndex;

        public bool IsAvailableTab => SelectedTabIndex == 0;
        public bool IsMyOrdersTab => SelectedTabIndex == 1;

        // ── Properties bound in CourierDashboardPage.xaml ─────────────────────────

        /// <summary>Badge showing how many deliveries are in progress.</summary>
        public int ActiveOrderCount => MyOrders.Count;

        /// <summary>Green dot when busy, amber when idle.</summary>
        public string StatusDotColor => MyOrders.Count > 0 ? "#5DBB7A" : "#C9A96E";

        /// <summary>
        /// Alias used by the XAML empty-state panel on the My Deliveries tab.
        /// True when the courier has no active deliveries.
        /// </summary>
        public bool HasNoOrders => HasNoMyOrders;

        // ── Init ──────────────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task Init()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                await LoadBothTabsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CourierVM] Init: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Tab switching ─────────────────────────────────────────────────────────

        [RelayCommand]
        public void SelectAvailableTab() => SelectedTabIndex = 0;

        [RelayCommand]
        public void SelectMyOrdersTab() => SelectedTabIndex = 1;

        // ── Accept order ──────────────────────────────────────────────────────────

        /// <summary>
        /// Courier taps "Accept Order" on an available order.
        /// Assigns them as the courier and moves status to Processing.
        /// </summary>
        [RelayCommand]
        public async Task AcceptOrder(Order order)
        {
            if (order == null || order.Status != "Pending") return;

            var confirmed = await Shell.Current.DisplayAlert(
                "Accept Order",
                $"Accept Order #{order.OrderId} for delivery to {order.Address}?",
                "Accept", "Cancel");

            if (!confirmed) return;

            try
            {
                IsBusy = true;

                var courierUid = _auth.IsUserLoggedIn
                    ? await SecureStorage.GetAsync("firebaseUid") ?? "unknown"
                    : "unknown";
                var courierName = _auth.CurrentUserDisplayName;

                var success = await _courierService.AssignCourierAsync(order, courierUid, courierName);

                if (success)
                {
                    AvailableOrders.Remove(order);
                    MyOrders.Insert(0, order);
                    HasNoAvailable = AvailableOrders.Count == 0;
                    HasNoMyOrders = MyOrders.Count == 0;

                    // Refresh computed properties so stats strip updates immediately
                    OnPropertyChanged(nameof(ActiveOrderCount));
                    OnPropertyChanged(nameof(StatusDotColor));
                    OnPropertyChanged(nameof(HasNoOrders));

                    SelectedTabIndex = 1; // switch to My Deliveries tab

                    await Shell.Current.DisplayAlert("✅ Accepted",
                        $"Order #{order.OrderId} is now yours.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error",
                        "Could not accept order. Try again.", "OK");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Advance status ────────────────────────────────────────────────────────

        /// <summary>
        /// Courier taps the status-advance button (e.g. "Mark as Shipped").
        /// Uses Order.CourierNextStatus to determine the new status.
        /// The "Out for Delivery → Delivered" step gets a stronger confirmation dialog.
        /// </summary>
        [RelayCommand]
        public async Task AdvanceStatus(Order order)
        {
            if (order == null || !order.CanCourierAdvance) return;

            // Final delivery step gets its own explicit confirmation
            bool isDeliveryConfirmation = order.Status == "Out for Delivery";

            string alertTitle = isDeliveryConfirmation
                ? "Confirm Delivery 📦"
                : "Update Status";

            string alertMessage = isDeliveryConfirmation
                ? $"Confirm that you have physically handed Order #{order.OrderId} " +
                  $"to {order.FullName} at:\n\n{order.Address}\n\nThis cannot be undone."
                : $"Confirm: {order.CourierActionLabel.Replace("⚙️  ", "").Replace("📦  ", "").Replace("🚚  ", "")} " +
                  $"for Order #{order.OrderId}?";

            string acceptButton = isDeliveryConfirmation ? "Yes, Delivered ✅" : "Yes";

            var confirmed = await Shell.Current.DisplayAlert(
                alertTitle, alertMessage, acceptButton, "Cancel");

            if (!confirmed) return;

            try
            {
                IsBusy = true;
                var success = await _courierService.UpdateOrderStatusAsync(order);
                if (!success)
                    await Shell.Current.DisplayAlert("Error",
                        "Status update failed. Check your connection.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Refresh ───────────────────────────────────────────────────────────────

        /// <summary>Bound to the ↺ refresh button in the header.</summary>
        [RelayCommand]
        private async Task Refresh() => await Init();

        // ── Navigation ────────────────────────────────────────────────────────────

        /// <summary>Bound to the ‹ back arrow in the header.</summary>
        [RelayCommand]
        private async Task NavigateBack()
            => await Shell.Current.Navigation.PopAsync();

        // ── Private ───────────────────────────────────────────────────────────────

        private async Task LoadBothTabsAsync()
        {
            // ── Available tab: Pending orders with no courier ─────────────────────
            var available = await _courierService.GetAvailableOrdersAsync();
            AvailableOrders.Clear();
            foreach (var o in available) AvailableOrders.Add(o);
            HasNoAvailable = AvailableOrders.Count == 0;

            // ── My Deliveries tab: orders assigned to this courier ────────────────
            var courierUid = await SecureStorage.GetAsync("firebaseUid") ?? string.Empty;
            var mine = await _courierService.GetMyActiveOrdersAsync(courierUid);
            MyOrders.Clear();
            foreach (var o in mine) MyOrders.Add(o);
            HasNoMyOrders = MyOrders.Count == 0;

            // Notify XAML-bound computed properties that depend on MyOrders
            OnPropertyChanged(nameof(ActiveOrderCount));
            OnPropertyChanged(nameof(StatusDotColor));
            OnPropertyChanged(nameof(HasNoOrders));
        }
    }
}