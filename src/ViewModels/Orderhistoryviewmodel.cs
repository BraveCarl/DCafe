// OrderHistoryViewModel.cs
// ─────────────────────────
// Backs OrderHistoryPage.
//
// DELIVERY CONFIRMATION RULES (per requirements):
//  • ConfirmOrder is the ONLY thing that changes Status → "Completed".
//  • Init() and LoadOrdersFromFirebaseAsync() are READ-ONLY — they never touch statuses.
//  • The "Confirm Received" button on the page is bound to ConfirmOrderCommand
//    and is only visible when Order.IsDelivered == true (Status == "Delivered").

using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCafe.Models;
using DCafe.Services;

namespace DCafe.ViewModels
{
    public partial class OrderHistoryViewModel : BaseViewModel
    {
        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly OrderService _orderService;

        private const string DbUrl =
            "https://coffeeshopapp-1ae15-default-rtdb.asia-southeast1.firebasedatabase.app";

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public OrderHistoryViewModel(OrderService orderService)
        {
            _orderService = orderService;
        }

        public OrderHistoryViewModel() { }

        // ── Data ──────────────────────────────────────────────────────────────
        public ObservableCollection<Order> Orders { get; } = new();

        [ObservableProperty]
        bool hasNoOrders;

        // ── Init ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads orders from OrderService + Firebase.
        /// Called automatically by EventToCommandBehavior (NavigatedTo).
        ///
        /// IMPORTANT: This method is READ-ONLY.
        /// It never changes order statuses — it only populates the list.
        /// </summary>
        [RelayCommand]
        public async Task Init()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Orders.Clear();

                // 1. Load in-memory orders (placed in this session)
                foreach (var order in _orderService.GetOrders())
                    Orders.Add(order);

                // 2. Merge orders saved in Firebase (previous sessions)
                await LoadOrdersFromFirebaseAsync();

                HasNoOrders = Orders.Count == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OrderHistoryVM] Init: {ex.Message}");
                HasNoOrders = Orders.Count == 0;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Confirm Received ──────────────────────────────────────────────────

        /// <summary>
        /// Called when the customer taps "✅ Confirm Received".
        ///
        /// RULES enforced here:
        ///  • The XAML button is only visible when IsDelivered (Status == "Delivered"),
        ///    so this guard is a safety net — not the primary gate.
        ///  • We always ask the customer to confirm via an alert before changing anything.
        ///  • Only on "Yes" do we call OrderService to push the status change to Firebase.
        /// </summary>
        [RelayCommand]
        public async Task ConfirmOrder(Order order)
        {
            // Guard: only allow confirmation when status is exactly "Delivered"
            if (order == null || order.Status != "Delivered") return;

            // Always ask first — never auto-confirm on navigation
            var confirmed = await Shell.Current.DisplayAlert(
                "Confirm Received",
                "Have you received your order?",
                "Yes, received",
                "Not yet");

            if (!confirmed) return;

            // Delegate the actual update to OrderService.
            // OrderService will:
            //   1. Set order.Status = "Completed" (local INPC fires → button disappears)
            //   2. Set order.ConfirmedAt = DateTime.Now
            //   3. PATCH the record in Firebase
            await _orderService.ConfirmReceivedAsync(order);
        }

        // ── Rate order ────────────────────────────────────────────────────────

        /// <summary>
        /// Lets the customer rate a Completed order.
        /// Button is only visible when CanRate (Status == "Completed" && !IsRated).
        /// </summary>
        [RelayCommand]
        public async Task RateOrder(Order order)
        {
            if (order == null) return;

            if (order.Status != "Completed")
            {
                await Shell.Current.DisplayAlert(
                    "Not available",
                    "You can only rate completed orders.",
                    "OK");
                return;
            }

            if (order.IsRated)
            {
                await Shell.Current.DisplayAlert(
                    "Already rated",
                    "You have already rated this order.",
                    "OK");
                return;
            }

            // Prompt for star rating (1–5)
            var ratingInput = await Shell.Current.DisplayPromptAsync(
                "Rate your order ⭐",
                $"Order #{order.OrderId}\nEnter a rating from 1 to 5:",
                placeholder: "e.g. 5",
                keyboard: Keyboard.Numeric,
                maxLength: 1);

            if (string.IsNullOrWhiteSpace(ratingInput)) return;

            if (!int.TryParse(ratingInput.Trim(), out int stars) || stars < 1 || stars > 5)
            {
                await Shell.Current.DisplayAlert(
                    "Invalid rating",
                    "Please enter a number between 1 and 5.",
                    "OK");
                return;
            }

            // Optional review text
            var review = await Shell.Current.DisplayPromptAsync(
                "Leave a review (optional)",
                "Share your experience:",
                placeholder: "Great coffee! ☕",
                maxLength: 200);

            // Update the order object (INPC fires, UI updates automatically)
            order.Rating = stars;
            order.Review = review?.Trim() ?? string.Empty;
            order.IsRated = true;

            System.Diagnostics.Debug.WriteLine(
                $"[OrderHistoryVM] Order #{order.OrderId} rated {stars}/5.");

            // Persist to Firebase
            await SaveRatingToFirebaseAsync(order);
        }

        // ── Firebase: load ────────────────────────────────────────────────────

        /// <summary>
        /// Fetches the current user's orders from Firebase and merges them
        /// into the Orders collection (skips duplicates by OrderId).
        ///
        /// READ-ONLY: does not write anything back to Firebase.
        /// </summary>
        private async Task LoadOrdersFromFirebaseAsync()
        {
            var uid = await SecureStorage.GetAsync("firebaseUid");
            if (string.IsNullOrEmpty(uid)) return;

            var response = await _http.GetAsync($"{DbUrl}/orders/{uid}.json");
            if (!response.IsSuccessStatusCode) return;

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null") return;

            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
            if (raw == null) return;

            var existingIds = Orders.Select(o => o.OrderId).ToHashSet();

            foreach (var kv in raw)
            {
                try
                {
                    var order = ParseOrder(kv.Value);
                    if (order == null || existingIds.Contains(order.OrderId)) continue;

                    // Insert in reverse-chronological order (newest first)
                    var insertAt = Orders
                        .Select((o, i) => (o, i))
                        .FirstOrDefault(x => x.o.PlacedAt < order.PlacedAt).i;

                    if (insertAt == 0 && Orders.Count > 0 && Orders[0].PlacedAt >= order.PlacedAt)
                        Orders.Add(order);
                    else
                        Orders.Insert(insertAt, order);

                    existingIds.Add(order.OrderId);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[OrderHistoryVM] Parse: {ex.Message}");
                }
            }
        }

        // ── Firebase: save rating ─────────────────────────────────────────────

        private async Task SaveRatingToFirebaseAsync(Order order)
        {
            try
            {
                var payload = new
                {
                    rating = order.Rating,
                    review = order.Review,
                    isRated = order.IsRated,
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{DbUrl}/orders/{order.UserId}/{order.OrderId}.json";

                var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
                var response = await _http.SendAsync(request);

                System.Diagnostics.Debug.WriteLine(response.IsSuccessStatusCode
                    ? $"[OrderHistoryVM] Rating saved for order #{order.OrderId}."
                    : $"[OrderHistoryVM] Rating save failed: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[OrderHistoryVM] SaveRatingToFirebase: {ex.Message}");
            }
        }

        // ── JSON parser ───────────────────────────────────────────────────────

        private static Order ParseOrder(JsonElement el)
        {
            string Get(string k) => el.TryGetProperty(k, out var p) ? p.GetString() ?? string.Empty : string.Empty;
            decimal Dec(string k) => el.TryGetProperty(k, out var p) && p.TryGetDecimal(out var d) ? d : 0m;
            int Int(string k) => el.TryGetProperty(k, out var p) && p.TryGetInt32(out var i) ? i : 0;
            bool Bool(string k) => el.TryGetProperty(k, out var p) && p.ValueKind == JsonValueKind.True;

            var order = new Order
            {
                OrderId = Get("orderId"),
                UserId = Get("userId"),
                FullName = Get("fullName"),
                PhoneNumber = Get("phoneNumber"),
                Address = Get("address"),
                PaymentMethod = Get("paymentMethod"),
                // Default to "Pending"/"Unpaid" if not stored yet
                Status = Get("status").Length > 0 ? Get("status") : "Pending",
                PaymentStatus = Get("paymentStatus").Length > 0 ? Get("paymentStatus") : "Unpaid",
                TotalAmount = Dec("totalAmount"),
                Rating = Int("rating"),
                Review = Get("review"),
                IsRated = Bool("isRated"),
            };

            if (DateTime.TryParse(Get("placedAt"), out var dt))
                order.PlacedAt = dt;

            if (el.TryGetProperty("items", out var itemsEl) &&
                itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsEl.EnumerateArray())
                {
                    string IGet(string k) => item.TryGetProperty(k, out var p) ? p.GetString() ?? string.Empty : string.Empty;
                    decimal IDec(string k) => item.TryGetProperty(k, out var p) && p.TryGetDecimal(out var d) ? d : 0m;
                    int IInt(string k) => item.TryGetProperty(k, out var p) && p.TryGetInt32(out var i) ? i : 0;

                    order.Items.Add(new OrderItem
                    {
                        ProductId = IInt("productId"),
                        ProductTitle = IGet("productTitle"),
                        ProductImage = IGet("productImage"),
                        UnitPrice = IDec("unitPrice"),
                        Quantity = IInt("quantity"),
                    });
                }
            }

            return string.IsNullOrEmpty(order.OrderId) ? null : order;
        }

        // ── Navigation ────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task NavigateBack()
            => await Shell.Current.Navigation.PopAsync();

        [RelayCommand]
        private async Task BackToMenu()
            => await Shell.Current.GoToAsync("//HomePage");
    }
}