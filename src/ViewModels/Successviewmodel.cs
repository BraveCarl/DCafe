using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCafe.Models;
using DCafe.Services;

namespace DCafe.ViewModels
{
    /// <summary>
    /// Receives the OrderId as a Shell query parameter, looks the Order up
    /// from the singleton OrderService, and exposes it for display.
    /// No business logic — purely presentation.
    /// </summary>
    [QueryProperty(nameof(OrderId), "OrderId")]
    public partial class SuccessViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;

        public SuccessViewModel(OrderService orderService)
        {
            _orderService = orderService;
        }

        public SuccessViewModel() { }

        // ── Incoming navigation parameter ─────────────────────────────────────────

        [ObservableProperty]
        string orderId;

        // When OrderId is set by Shell navigation, load the matching Order
        partial void OnOrderIdChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var order = _orderService.GetOrders()
                                     .FirstOrDefault(o => o.OrderId == value);
            if (order != null)
                LoadOrder(order);
        }

        // ── Display properties ────────────────────────────────────────────────────

        [ObservableProperty] string displayOrderId;
        [ObservableProperty] string fullName;
        [ObservableProperty] string address;
        [ObservableProperty] string phoneNumber;
        [ObservableProperty] string paymentMethod;
        [ObservableProperty] string status;
        [ObservableProperty] string placedAt;
        [ObservableProperty] string totalAmount;
        [ObservableProperty] string itemsSummary;

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void LoadOrder(Order order)
        {
            DisplayOrderId = $"#{order.OrderId}";
            FullName = order.FullName;
            Address = order.Address;
            PhoneNumber = order.PhoneNumber;
            PaymentMethod = order.PaymentMethod;
            Status = order.Status;
            PlacedAt = order.PlacedAt.ToString("MMM dd, yyyy  h:mm tt");
            TotalAmount = $"₱{order.TotalAmount:F2}";
            ItemsSummary = string.Join("\n", order.Items
                .Select(i => $"• {i.ProductTitle}  x{i.Quantity}  —  ₱{i.LineTotal:F2}"));
        }

        // ── Commands ──────────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task BackToHome()
            => await Shell.Current.GoToAsync("//HomePage");

        [RelayCommand]
        private async Task ViewOrders()
            => await Shell.Current.GoToAsync("OrderHistoryPage");
    }
}