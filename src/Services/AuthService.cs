
using MauiStoreApp.Models;

using System.Text;
using System.Text.Json;

namespace MauiStoreApp.Services
{



    /// <summary>
    /// This class is responsible for authenticating the user and storing the token and userId in secure storage.
    /// </summary>
    public class AuthService : BaseService
    {
        private string _cachedToken;
        private string _cachedFirebaseUid;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        public AuthService()
        {
        }

        // ADD THIS — called by LoginViewModel after successful Firebase login
        public void NotifyFirebaseLogin(string firebaseUid)
        {
            _cachedFirebaseUid = firebaseUid;
        }

        /// <summary>
        /// Initialize cached values from SecureStorage. Call this at app startup.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _cachedToken = await SecureStorage.GetAsync("token");

                _cachedFirebaseUid = await SecureStorage.GetAsync("firebaseUid");
            }
            catch { }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user is logged in.
        /// </summary>
        /// <remarks>
        /// Uses cached token to avoid blocking the UI thread.
        /// </remarks>
        public bool IsUserLoggedIn
        {
            get => !string.IsNullOrEmpty(_cachedToken) || !string.IsNullOrEmpty(_cachedFirebaseUid);
            set
            {
                if (!value)
                {
                    _cachedToken = null;
                    SecureStorage.Remove("token");
                    SecureStorage.Remove("userId");
                    SecureStorage.Remove("firebaseUid");      // ADD
                    SecureStorage.Remove("firebaseToken");    // ADD
                    SecureStorage.Remove("firebaseEmail");    // ADD
                    SecureStorage.Remove("displayName");
                }
            }
        }


        /// <summary>
        /// Logs the user in.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>A task of type <see cref="LoginResponse"/>.</returns>
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password,
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("auth/login", content);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent);

            // fetch all users
            var usersResponse = await _httpClient.GetAsync("users");
            usersResponse.EnsureSuccessStatusCode();
            var usersResponseContent = await usersResponse.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<User>>(usersResponseContent);

            // find the matching user and set the UserId in LoginResponse
            var user = users.FirstOrDefault(u => u.Username == username);
            if (user != null)
            {
                loginResponse.UserId = user.Id;
            }

            // Update cache after successful login
            _cachedToken = loginResponse?.Token;

            return loginResponse;
        }
    }
}
