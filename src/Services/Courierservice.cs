using System.Text;
using System.Text.Json;
using MauiStoreApp.Models;

namespace MauiStoreApp.Services
{
    /// <summary>
    /// Handles all courier-side operations:
    ///   - Fetching available (unassigned) orders
    ///   - Fetching this courier's active deliveries
    ///   - Accepting an order (assigning self)
    ///   - Advancing order status
    ///
    /// Firebase structure used:
    ///   /orders/{userId}/{orderId}.json      ← full order (existing)
    ///   /all_orders/{orderId}.json           ← flat index (lightweight, added on PlaceOrder)
    ///
    /// The flat /all_orders index is the key that lets a courier query orders
    /// without knowing every customer's UID.
    /// </summary>
    public class CourierService
    {
        private const string DbUrl =
            "https://coffeeshopapp-1ae15-default-rtdb.asia-southeast1.firebasedatabase.app";

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        // ── Available orders (Pending, no courier yet) ────────────────────────────

        /// <summary>
        /// Returns all orders that are still Pending and have no courier assigned.
        /// Couriers see these on the "Available" tab and can accept one.
        /// </summary>
        public async Task<List<Order>> GetAvailableOrdersAsync()
        {
            try
            {
                var response = await _http.GetAsync($"{DbUrl}/all_orders.json");
                if (!response.IsSuccessStatusCode) return new();

                var body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null") return new();

                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                if (raw == null) return new();

                var result = new List<Order>();
                foreach (var kv in raw)
                {
                    var order = ParseOrderIndex(kv.Value);
                    if (order != null && order.Status == "Pending" && !order.IsAssigned)
                        result.Add(order);
                }

                return result.OrderByDescending(o => o.PlacedAt).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CourierService] GetAvailable: {ex.Message}");
                return new();
            }
        }

        // ── My active deliveries ──────────────────────────────────────────────────

        /// <summary>
        /// Returns orders assigned to this courier that are not yet Completed.
        /// Couriers see these on the "My Deliveries" tab.
        /// </summary>
        public async Task<List<Order>> GetMyActiveOrdersAsync(string courierUid)
        {
            try
            {
                var response = await _http.GetAsync($"{DbUrl}/all_orders.json");
                if (!response.IsSuccessStatusCode) return new();

                var body = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null") return new();

                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                if (raw == null) return new();

                var result = new List<Order>();
                foreach (var kv in raw)
                {
                    var order = ParseOrderIndex(kv.Value);
                    if (order != null &&
                        order.CourierUid == courierUid &&
                        order.Status != "Completed")
                        result.Add(order);
                }

                return result.OrderByDescending(o => o.PlacedAt).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CourierService] GetMyOrders: {ex.Message}");
                return new();
            }
        }

        // ── Accept order ──────────────────────────────────────────────────────────

        /// <summary>
        /// Courier accepts a Pending order:
        ///   1. Sets courierUid + courierName on the order
        ///   2. Advances status to "Processing"
        ///   3. Patches both /orders/{userId}/{orderId} and /all_orders/{orderId}
        /// </summary>
        public async Task<bool> AssignCourierAsync(Order order, string courierUid, string courierName)
        {
            if (order == null || order.Status != "Pending") return false;

            try
            {
                var patch = new Dictionary<string, object>
                {
                    ["courierUid"] = courierUid,
                    ["courierName"] = courierName,
                    ["status"] = "Processing",
                };

                // Patch full order node
                var ok1 = await PatchAsync($"{DbUrl}/orders/{order.UserId}/{order.OrderId}.json", patch);

                // Patch flat index
                var ok2 = await PatchAsync($"{DbUrl}/all_orders/{order.OrderId}.json", patch);

                if (ok1 && ok2)
                {
                    // Update in-memory object so UI reflects immediately via INPC
                    order.CourierUid = courierUid;
                    order.CourierName = courierName;
                    order.Status = "Processing";
                    System.Diagnostics.Debug.WriteLine(
                        $"[CourierService] Order #{order.OrderId} accepted by {courierName}.");
                }

                return ok1 && ok2;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CourierService] AssignCourier: {ex.Message}");
                return false;
            }
        }

        // ── Advance status ────────────────────────────────────────────────────────

        /// <summary>
        /// Moves the order to its next status in the courier flow:
        ///   Processing → Shipped → Out for Delivery → Delivered
        ///
        /// Uses Order.CourierNextStatus (computed property) so there's no
        /// hardcoded status string here.
        /// </summary>
        public async Task<bool> UpdateOrderStatusAsync(Order order)
        {
            var nextStatus = order.CourierNextStatus;
            if (string.IsNullOrEmpty(nextStatus)) return false;

            try
            {
                var patch = new Dictionary<string, object> { ["status"] = nextStatus };

                var ok1 = await PatchAsync($"{DbUrl}/orders/{order.UserId}/{order.OrderId}.json", patch);
                var ok2 = await PatchAsync($"{DbUrl}/all_orders/{order.OrderId}.json", patch);

                if (ok1 && ok2)
                {
                    order.Status = nextStatus;   // INPC → UI updates live
                    System.Diagnostics.Debug.WriteLine(
                        $"[CourierService] Order #{order.OrderId} → {nextStatus}");
                }

                return ok1 && ok2;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CourierService] UpdateStatus: {ex.Message}");
                return false;
            }
        }

        // ── Firebase helpers ──────────────────────────────────────────────────────

        private async Task<bool> PatchAsync(string url, Dictionary<string, object> data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var req = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
            var resp = await _http.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }

        /// <summary>
        /// Writes the lightweight index entry to /all_orders/{orderId}.json.
        /// Called by OrderService immediately after a new order is saved.
        /// Stores only the fields the courier needs to filter orders —
        /// not the full item list (that stays in /orders/{userId}/{orderId}).
        /// </summary>
        public async Task SaveOrderIndexAsync(Order order)
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
                    status = order.Status,
                    courierUid = order.CourierUid ?? string.Empty,
                    courierName = order.CourierName ?? string.Empty,
                    totalAmount = order.TotalAmount,
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _http.PutAsync($"{DbUrl}/all_orders/{order.OrderId}.json", content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CourierService] SaveIndex: {ex.Message}");
            }
        }

        // ── Parse flat index entry ────────────────────────────────────────────────

        private static Order ParseOrderIndex(JsonElement el)
        {
            string G(string k) => el.TryGetProperty(k, out var p) ? p.GetString() ?? string.Empty : string.Empty;
            decimal D(string k) => el.TryGetProperty(k, out var p) && p.TryGetDecimal(out var d) ? d : 0m;

            var order = new Order
            {
                OrderId = G("orderId"),
                UserId = G("userId"),
                FullName = G("fullName"),
                PhoneNumber = G("phoneNumber"),
                Address = G("address"),
                Status = G("status").Length > 0 ? G("status") : "Pending",
                CourierUid = G("courierUid"),
                CourierName = G("courierName"),
                TotalAmount = D("totalAmount"),
            };

            if (DateTime.TryParse(G("placedAt"), out var dt))
                order.PlacedAt = dt;

            return string.IsNullOrEmpty(order.OrderId) ? null : order;
        }
    }
}