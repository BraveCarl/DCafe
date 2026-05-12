using System.Diagnostics;

namespace MauiStoreApp.Services
{
    /// <summary>
    /// This class provides base functionality for other service classes.
    /// </summary>
    public class BaseService
    {
        /// <summary>
        /// An instance of <see cref="HttpClient"/>.
        /// </summary>
        protected readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseService"/> class.
        /// </summary>
        public BaseService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://coffeeshopapp-1ae15-default-rtdb.asia-southeast1.firebasedatabase.app/"),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        /// <summary>
        /// Sends a GET request to the specified endpoint and returns the response as an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="endpoint">The endpoint to send the GET request to.</param>
        /// <returns>A task of type <typeparamref name="T"/>.</returns>
        protected async Task<T> GetAsync<T>(string endpoint)
        {
            if (!IsInternetAvailable())
            {
                return default;
            }

            try
            {
                var response = await _httpClient.GetAsync(endpoint);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<T>(responseContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get data: {ex.Message}");
                try { await Shell.Current?.DisplayAlert("Error!", "Unable to get data.", "OK"); } catch { }
                return default;
            }
        }

        /// <summary>
        /// Sends a GET request and returns the raw JSON response body as a string.
        /// Use this when you need flexible deserialization (e.g. Firebase may return
        /// a keyed object or an array depending on how the data was stored).
        /// </summary>
        /// <param name="endpoint">The endpoint to send the GET request to.</param>
        /// <returns>The raw JSON string, or null on failure.</returns>
        protected async Task<string> GetRawJsonAsync(string endpoint)
        {
            if (!IsInternetAvailable())
            {
                return null;
            }

            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to get data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sends a DELETE request to the specified endpoint and returns the response.
        /// </summary>
        /// <param name="endpoint">The endpoint to send the DELETE request to.</param>
        /// <returns>A task of type <see cref="HttpResponseMessage"/>.</returns>
        protected async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            if (!IsInternetAvailable())
            {
                return null;
            }

            try
            {
                return await _httpClient.DeleteAsync(endpoint);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to delete data: {ex.Message}");
                try { await Shell.Current?.DisplayAlert("Error!", "Unable to delete data.", "OK"); } catch { }
                return null;
            }
        }

        /// <summary>
        /// Checks if an internet connection is available (non-blocking).
        /// </summary>
        /// <returns><c>true</c> if an internet connection is available; otherwise, <c>false</c>.</returns>
        private bool IsInternetAvailable()
        {
            try
            {
                // Quick check - avoid blocking the main thread
                NetworkAccess accessType;
                try
                {
                    accessType = Connectivity.NetworkAccess;
                }
                catch
                {
                    // If connectivity check fails, assume we have internet and let HTTP timeout handle it
                    return true;
                }

                if (accessType != NetworkAccess.Internet)
                {
                    // Fire and forget - don't wait for alert
                    _ = ShowNoInternetAlertAsync(accessType);
                    return false;
                }
                return true;
            }
            catch
            {
                // Assume internet is available, let HTTP request fail naturally with timeout
                return true;
            }
        }

        private static async Task ShowNoInternetAlertAsync(NetworkAccess accessType)
        {
            try
            {
                // Small delay to ensure Shell is ready
                await Task.Delay(100);
                if (Shell.Current != null)
                {
                    var msg = accessType == NetworkAccess.ConstrainedInternet
                        ? "Internet access is limited."
                        : "No internet access.";
                    await Shell.Current.DisplayAlert("Error!", msg, "OK");
                }
            }
            catch { }
        }
    }
}
