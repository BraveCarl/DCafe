using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// CRASH FIX: Removed "using Firebase.Auth;" — this namespace does not exist
// in a REST-only setup and causes a build failure.

namespace DCafe.Services
{
    public class FirebaseAuthService
    {
        private const string ApiKey = "AIzaSyBzkqtO1V9fnAOHz1QfwsBJ1D0jws0XtLI";
        private const string BaseUrl = "https://identitytoolkit.googleapis.com/v1/accounts";

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        private string _cachedUid;
        private string _cachedEmail;
        private string _cachedDisplayName;

        public bool IsUserLoggedIn => !string.IsNullOrEmpty(_cachedUid);

        public string CurrentUserEmail => _cachedEmail ?? string.Empty;

        public string CurrentUserDisplayName => _cachedDisplayName ?? string.Empty;

        public async Task InitializeAsync()
        {
            try
            {
                _cachedUid = await SecureStorage.GetAsync("firebaseUid");
                _cachedEmail = await SecureStorage.GetAsync("firebaseEmail");
                _cachedDisplayName = await SecureStorage.GetAsync("displayName");
            }
            catch { }  // SecureStorage may fail on some devices
        }

        public async Task<(bool Success, string Error)> RegisterAsync(
            string username, string email, string password)
        {
            try
            {
                var payload = new { email, password, returnSecureToken = true };
                var response = await PostAsync($"{BaseUrl}:signUp?key={ApiKey}", payload);

                if (!response.IsSuccessStatusCode)
                    return (false, await ParseFirebaseError(response));

                var result = JsonSerializer.Deserialize<FirebaseSignInResponse>(
                    await response.Content.ReadAsStringAsync());

                await SecureStorage.Default.SetAsync("firebaseUid", result.LocalId);
                await SecureStorage.Default.SetAsync("firebaseToken", result.IdToken);
                await SecureStorage.Default.SetAsync("displayName", username);
                await SecureStorage.Default.SetAsync("firebaseEmail", email);
                await UpdateDisplayNameAsync(result.IdToken, username);

                // Update cache
                _cachedUid = result.LocalId;
                _cachedEmail = email;
                _cachedDisplayName = username;

                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string Error)> LoginAsync(string email, string password)
        {
            try
            {
                var payload = new { email, password, returnSecureToken = true };
                var response = await PostAsync(
                    $"{BaseUrl}:signInWithPassword?key={ApiKey}", payload);

                if (!response.IsSuccessStatusCode)
                    return (false, await ParseFirebaseError(response));

                var result = JsonSerializer.Deserialize<FirebaseSignInResponse>(
                    await response.Content.ReadAsStringAsync());

                await SecureStorage.Default.SetAsync("firebaseUid", result.LocalId);
                await SecureStorage.Default.SetAsync("firebaseToken", result.IdToken);
                await SecureStorage.Default.SetAsync("displayName", result.DisplayName ?? email);
                await SecureStorage.Default.SetAsync("firebaseEmail", result.Email ?? email);

                // Update cache
                _cachedUid = result.LocalId;
                _cachedEmail = result.Email ?? email;
                _cachedDisplayName = result.DisplayName ?? email;

                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public Task LogoutAsync()
        {
            SecureStorage.Default.Remove("firebaseUid");
            SecureStorage.Default.Remove("firebaseToken");
            SecureStorage.Default.Remove("displayName");
            SecureStorage.Default.Remove("firebaseEmail");
            
            // Clear cache
            _cachedUid = null;
            _cachedEmail = null;
            _cachedDisplayName = null;
            
            return Task.CompletedTask;
        }

        public async Task<bool> ReAuthenticateAsync(string email, string password)
        {
            var response = await PostAsync(
                $"{BaseUrl}:signInWithPassword?key={ApiKey}",
                new { email, password, returnSecureToken = true });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAccountAsync()
        {
            var idToken = await SecureStorage.GetAsync("firebaseToken");
            if (string.IsNullOrEmpty(idToken)) return false;
            var response = await PostAsync(
                $"{BaseUrl}:delete?key={ApiKey}", new { idToken });
            return response.IsSuccessStatusCode;
        }

        private async Task UpdateDisplayNameAsync(string idToken, string displayName)
        {
            try
            {
                await PostAsync($"{BaseUrl}:update?key={ApiKey}",
                new { idToken, displayName, returnSecureToken = false });
            }
            catch { }
        }

        private async Task<HttpResponseMessage> PostAsync(string url, object payload)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            return await _http.PostAsync(url, content);
        }

        private static async Task<string> ParseFirebaseError(HttpResponseMessage response)
        {
            try
            {
                var error = JsonSerializer.Deserialize<FirebaseErrorResponse>(
                    await response.Content.ReadAsStringAsync());
                return error?.Error?.Message switch
                {
                    "EMAIL_EXISTS" => "An account with this email already exists.",
                    "WEAK_PASSWORD" => "Password must be at least 6 characters.",
                    "INVALID_EMAIL" => "Please enter a valid email address.",
                    "EMAIL_NOT_FOUND" => "No account found with that email.",
                    "INVALID_PASSWORD" => "Incorrect password. Please try again.",
                    "INVALID_LOGIN_CREDENTIALS" => "Invalid email or password.",
                    "USER_DISABLED" => "This account has been disabled.",
                    "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many attempts. Please try again later.",
                    var msg => msg ?? "An unknown error occurred."
                };
            }
            catch { return "An unknown error occurred."; }
        }

        private class FirebaseSignInResponse
        {
            [JsonPropertyName("idToken")] public string IdToken { get; set; }
            [JsonPropertyName("email")] public string Email { get; set; }
            [JsonPropertyName("localId")] public string LocalId { get; set; }
            [JsonPropertyName("displayName")] public string DisplayName { get; set; }
        }

        private class FirebaseErrorResponse
        {
            [JsonPropertyName("error")] public FirebaseErrorDetail Error { get; set; }
        }

        private class FirebaseErrorDetail
        {
            [JsonPropertyName("message")] public string Message { get; set; }
        }
    }
}