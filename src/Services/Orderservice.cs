using System.Text;
using System.Text.Json;
using MauiStoreApp.Models;

namespace MauiStoreApp.Services
{
    public class OrderService
    {
        private readonly CartService _cartService;
        private readonly CourierService _courierService;   // NEW: to write /all_orders index
        private readonly List<Order> _orders = new();

        private const string DbUrl =
            "https://coffeeshopapp-1ae15-default-rtdb.asia-southeast1.firebasedatabase.app";

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public OrderService(CartService cartService, CourierService courierService)
        {
            _cartService = cartService;
            _courierService = courierService;
        }

        // ── Read ──────────────────────────────────────────────────────────────────

        public IReadOnlyList<Order> GetOrders()
            => _orders.OrderByDescending(o => o.PlacedAt).ToList();

        // ── Place order ───────────────────────────────────────────────────────────

        public async Task<(bool Success, Order Order, string Error)> PlaceOrderAsync(
            string fullName, string phoneNumber, string address,
            string paymentMethod = "COD", string referenceNumber = null)
        {
            var cartItems = _cartService.GetCartItems();

            if (cartItems.Count == 0)
                return (false, null, "Your cart is empty.");
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, null, "Full name is required.");
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Trim().Length < 7)
                return (false, null, "Please enter a valid phone number.");
            if (string.IsNullOrWhiteSpace(address))
                return (false, null, "Delivery address is required.");
            if (paymentMethod == "GCash" && string.IsNullOrWhiteSpace(referenceNumber))
                return (false, null, "Reference number is required for GCash payment.");

            var userId =
                await SecureStorage.GetAsync("firebaseUid") ??
                await SecureStorage.GetAsync("userId") ??
                "guest";

            var isGCash = paymentMethod == "GCash";

            var order = new Order
            {
                UserId = userId,
                FullName = fullName.Trim(),
                PhoneNumber = phoneNumber.Trim(),
                Address = address.Trim(),
                PaymentMethod = paymentMethod,
                ReferenceNumber = isGCash ? referenceNumber?.Trim() : null,
                Status = isGCash ? "For Verification" : "Pending",
                PaymentStatus = isGCash ? "Paid" : "Unpaid",
                TotalAmount = cartItems.Sum(i => i.Product.Price * i.Quantity),
                Items = cartItems.Select(i => new OrderItem
                {
                    ProductId = i.Product.Id,
                    ProductTitle = i.Product.Title,
                    ProductImage = i.Product.Image,
                    UnitPrice = i.Product.Price,
                    Quantity = i.Quantity,
                }).ToList(),
            };

            _orders.Add(order);
            await SaveOrderToFirebaseAsync(order);
            await _courierService.SaveOrderIndexAsync(order);
            ClearCart();

            // Status progression is now 100% controlled by the courier via CourierDashboardViewModel.
            // ProgressStatusAsync is intentionally NOT called here.

            return (true, order, null);
        }

        // ── Status lifecycle ─────────────────────────────o─────────────────────────

        public async Task ProgressStatusAsync(Order order)
        {
            try
            {
                // Pending -> Processing
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (order.Status != "Pending") return;

                order.Status = "Processing";

                await PatchOrderFieldAsync(
                    order.UserId,
                    order.OrderId,
                    "status",
                    "Processing");

                // Processing -> Shipped
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (order.Status != "Processing") return;

                order.Status = "Shipped";

                await PatchOrderFieldAsync(
                    order.UserId,
                    order.OrderId,
                    "status",
                    "Shipped");

                // Shipped -> Out for Delivery
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (order.Status != "Shipped") return;

                order.Status = "Out for Delivery";

                await PatchOrderFieldAsync(
                    order.UserId,
                    order.OrderId,
                    "status",
                    "Out for Delivery");

                // Out for Delivery -> Delivered
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (order.Status != "Out for Delivery") return;

                order.Status = "Delivered";
                order.DeliveredAt = DateTime.Now;

                await PatchOrderFieldAsync(
                    order.UserId,
                    order.OrderId,
                    "status",
                    "Delivered");

                // Optional timestamp save
                await PatchOrderFieldAsync(
                    order.UserId,
                    order.OrderId,
                    "deliveredAt",
                    order.DeliveredAt?.ToString("o"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[OrderService] ProgressStatus: {ex.Message}");
            }
        }

        public async Task AutoCompleteOrderAsync(Order order)
        {
            if (order.Status != "Delivered" || order.PaymentMethod != "COD") return;

            await Task.Delay(TimeSpan.FromSeconds(60));

            if (order.Status == "Delivered")
            {
                order.PaymentStatus = "Paid";
                order.Status = "Completed";
                await PatchOrderFieldAsync(order.UserId, order.OrderId, "status", "Completed");
                await PatchOrderFieldAsync(order.UserId, order.OrderId, "paymentStatus", "Paid");
                System.Diagnostics.Debug.WriteLine($"[OrderService] Order #{order.OrderId} auto-completed.");
            }
        }

        public async Task ConfirmReceivedAsync(Order order)
        {
            if (order.Status != "Delivered") return;

            order.PaymentStatus = "Paid";
            order.Status = "Completed";
            order.ConfirmedAt = DateTime.Now;   // ADD THIS LINE

            await PatchOrderFieldAsync(order.UserId, order.OrderId, "status", "Completed");
            await PatchOrderFieldAsync(order.UserId, order.OrderId, "paymentStatus", "Paid");
            await PatchOrderFieldAsync(order.UserId, order.OrderId, "confirmedAt",   // ADD THIS
                order.ConfirmedAt.Value.ToString("o"));

            System.Diagnostics.Debug.WriteLine(
                $"[OrderService] Order #{order.OrderId} confirmed by customer.");
        }

        // ── Courier methods (NEW) ─────────────────────────────────────────────────────

        /// <summary>
        /// Called by CourierService when a courier accepts a Pending order.
        /// Sets the courier identity and advances status to Processing.
        /// </summary>
        public async Task AssignCourierAsync(Order order, string courierUid, string courierName)
        {
            if (order == null || order.Status != "Pending") return;

            order.CourierUid = courierUid;
            order.CourierName = courierName;
            order.Status = "Processing";

            // Patch all three fields in one call each — reuses the existing private method
            await PatchOrderFieldAsync(order.UserId, order.OrderId, "courierUid", courierUid);
            await PatchOrderFieldAsync(order.UserId, order.OrderId, "courierName", courierName);
            await PatchOrderFieldAsync(order.UserId, order.OrderId, "status", "Processing");

            System.Diagnostics.Debug.WriteLine(
                $"[OrderService] Order #{order.OrderId} assigned to {courierName}.");
        }

        /// <summary>
        /// Called by CourierService when courier taps the advance-status button.
        /// Uses Order.CourierNextStatus so no status string is hardcoded here.
        /// Flow: Processing → Shipped → Out for Delivery → Delivered
        /// </summary>
        public async Task UpdateDeliveryStatusAsync(Order order)
        {
            var next = order.CourierNextStatus;
            if (string.IsNullOrEmpty(next)) return;

            order.Status = next;
            await PatchOrderFieldAsync(order.UserId, order.OrderId, "status", next);

            System.Diagnostics.Debug.WriteLine(
                $"[OrderService] Order #{order.OrderId} → {next}");
        }

        /// <summary>
        /// Called when courier taps "Mark as Delivered".
        /// Sets status + stamps the DeliveredAt timestamp, then starts the
        /// 60-second auto-complete fallback for COD orders.
        /// </summary>
        public async Task MarkAsDeliveredAsync(Order order)
        {
            if (order == null || order.Status != "Out for Delivery") return;

            order.Status = "Delivered";
            order.DeliveredAt = DateTime.Now;

            await PatchOrderFieldAsync(order.UserId, order.OrderId, "status", "Delivered");
            await PatchOrderFieldAsync(order.UserId, order.OrderId, "deliveredAt", order.DeliveredAt.Value.ToString("o"));

            System.Diagnostics.Debug.WriteLine(
                $"[OrderService] Order #{order.OrderId} delivered at {order.DeliveredAt}.");

            // Start the 60s fallback — if customer doesn't confirm, auto-complete fires
            _ = AutoCompleteOrderAsync(order);
        }
        public void ClearOrders() => _orders.Clear();

        // ── Firebase persistence ──────────────────────────────────────────────────

        private async Task SaveOrderToFirebaseAsync(Order order)
        {
            try
            {
                var payload = new
                {
                    orderId = order.OrderId,
                    userId = order.UserId,
                    placedAt = order.PlacedAt.ToString("o"),
                    fullName = order.FullName,
                    phoneNumber = order.PhoneNumber,
                    address = order.Address,
                    paymentMethod = order.PaymentMethod,
                    referenceNumber = order.ReferenceNumber,
                    paymentStatus = order.PaymentStatus,
                    status = order.Status,
                    courierUid = order.CourierUid ?? string.Empty,
                    courierName = order.CourierName ?? string.Empty,
                    deliveredAt = order.DeliveredAt?.ToString("o") ?? string.Empty,
                    confirmedAt = order.ConfirmedAt?.ToString("o") ?? string.Empty,
                    totalAmount = order.TotalAmount,
                    items = order.Items.Select(i => new
                    {
                        productId = i.ProductId,
                        productTitle = i.ProductTitle,
                        productImage = i.ProductImage,
                        unitPrice = i.UnitPrice,
                        quantity = i.Quantity,
                        lineTotal = i.LineTotal,
                    }),
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{DbUrl}/orders/{order.UserId}/{order.OrderId}.json";
                var response = await _http.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                    System.Diagnostics.Debug.WriteLine(
                        $"[OrderService] Firebase save failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderService] SaveToFirebase: {ex.Message}");
            }
        }

        private async Task PatchOrderFieldAsync(
            string userId, string orderId, string field, string value)
        {
            try
            {
                var json = JsonSerializer.Serialize(new Dictionary<string, string> { [field] = value });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{DbUrl}/orders/{userId}/{orderId}.json";
                var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
                await _http.SendAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderService] PatchField: {ex.Message}");
            }
        }

        // ── Cart clear ────────────────────────────────────────────────────────────

        private void ClearCart()
        {
            var items = _cartService.GetCartItems().ToList();
            foreach (var item in items)
            {
                item.Quantity = 1;
                _cartService.DecreaseProductQuantity(item.Product.Id);
            }
        }
    }
}