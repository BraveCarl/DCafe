using System.Text;
using System.Text.Json;

namespace DCafe.Services
{
    /// <summary>
    /// Stores and retrieves user profile data using the Firebase Realtime Database REST API.
    ///
    /// CRASH FIX: The original file used Google.Cloud.Firestore (the server-side Admin SDK).
    /// That SDK requires gRPC server infrastructure and throws PlatformNotSupportedException
    /// on Android/iOS at startup — crashing the app before any page loads.
    /// This version uses plain HttpClient REST calls, identical to FirebaseAuthService.
    /// </summary>
    public class FirebaseUserService
    {
        // Same Realtime Database URL already used in your DeleteUserProfile method
        private const string DbUrl =
            "https://coffeeshopapp-1ae15-default-rtdb.asia-southeast1.firebasedatabase.app";

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        // ── Save ──────────────────────────────────────────────────────────────────

        public async Task SaveUserProfile(string uid, Dictionary<string, object> data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // PATCH merges fields; PUT would overwrite the entire node
                var request = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"{DbUrl}/users/{uid}.json")
                {
                    Content = content
                };

                await _http.SendAsync(request);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FirebaseUserService] SaveUserProfile: {ex.Message}");
            }
        }

        // ── Get ───────────────────────────────────────────────────────────────────

        /// <summary>Returns the user's profile fields, or null if the node does not exist.</summary>
        public async Task<Dictionary<string, object>> GetUserProfile(string uid)
        {
            try
            {
                var response = await _http.GetAsync($"{DbUrl}/users/{uid}.json");

                if (!response.IsSuccessStatusCode) return null;

                var body = await response.Content.ReadAsStringAsync();

                // Realtime DB returns the JSON literal "null" when the node is missing
                if (string.IsNullOrWhiteSpace(body) || body.Trim() == "null")
                    return null;

                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                if (raw == null) return null;

                // Convert JsonElement values to plain objects so callers get strings
                var result = new Dictionary<string, object>();
                foreach (var kv in raw)
                    result[kv.Key] = kv.Value.ValueKind == JsonValueKind.String
                        ? kv.Value.GetString()
                        : kv.Value.ToString();

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FirebaseUserService] GetUserProfile: {ex.Message}");
                return null;
            }
        }


        // ADD this method — fetches only the role field instead of the full profile
        // Placed between GetUserProfile() and DeleteUserProfile()
        public async Task<string> GetUserRoleAsync(string uid)
        {
            try
            {
                var response = await _http.GetAsync($"{DbUrl}/users/{uid}/role.json");
                if (!response.IsSuccessStatusCode) return "customer";

                var body = await response.Content.ReadAsStringAsync();

                // Firebase wraps string values in quotes: "\"courier\""
                var role = body.Trim().Trim('"').ToLower();

                return string.IsNullOrWhiteSpace(role) || role == "null"
                    ? "customer"
                    : role;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FirebaseUserService] GetUserRole: {ex.Message}");
                return "customer";  // safe default — never crashes the login flow
            }
        }


        // ── Delete ────────────────────────────────────────────────────────────────

        public async Task DeleteUserProfile(string uid)
        {
            try
            {
                await _http.DeleteAsync($"{DbUrl}/users/{uid}.json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FirebaseUserService] DeleteUserProfile: {ex.Message}");
            }
        }
    }
}