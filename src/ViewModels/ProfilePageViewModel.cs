using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DCafe.Models;
using DCafe.Services;
using DCafe.Views;

namespace DCafe.ViewModels
{
    public partial class ProfilePageViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private readonly AuthService _authService;
        private readonly FirebaseAuthService _firebaseAuth;
        private readonly FirebaseUserService _firebaseUserService;

        public ProfilePageViewModel(
            UserService userService,
            AuthService authService,
            FirebaseAuthService firebaseAuth,
            FirebaseUserService firebaseUserService)
        {
            _userService = userService;
            _authService = authService;
            _firebaseAuth = firebaseAuth;
            _firebaseUserService = firebaseUserService;
        }

        public ProfilePageViewModel() { }

        [ObservableProperty] bool isUserLoggedIn;
        [ObservableProperty] FirebaseUser user;
        [ObservableProperty] string firebaseDisplayName;
        [ObservableProperty] string firebaseEmail;

        // ── Init ──────────────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task Init()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var firebaseUid = await SecureStorage.GetAsync("firebaseUid");
                IsUserLoggedIn = !string.IsNullOrEmpty(firebaseUid);

                if (!IsUserLoggedIn)
                {
                    User = null;
                    FirebaseDisplayName = string.Empty;
                    FirebaseEmail = string.Empty;
                    return;
                }

                FirebaseDisplayName = await SecureStorage.GetAsync("displayName") ?? "User";
                FirebaseEmail = _firebaseAuth.CurrentUserEmail ?? string.Empty;

                var data = await _firebaseUserService.GetUserProfile(firebaseUid);

                User = new FirebaseUser
                {
                    FullName = GetStringOrFallback(data, "fullName", FirebaseDisplayName),
                    Phone = GetStringOrFallback(data, "phone", string.Empty),
                    Street = GetStringOrFallback(data, "street", string.Empty),
                    City = GetStringOrFallback(data, "city", string.Empty),
                    Province = GetStringOrFallback(data, "province", string.Empty),
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProfilePageViewModel] Init: {ex.Message}");
                User ??= new FirebaseUser { FullName = "User" };
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Account ───────────────────────────────────────────────────────────────

        [RelayCommand]
        private async Task Logout()
        {
            var confirmed = await Shell.Current.DisplayAlert(
                "Logout", "Are you sure you want to log out?", "Yes", "No");
            if (!confirmed) return;

            await _firebaseAuth.LogoutAsync();
            _authService.IsUserLoggedIn = false;
            await Shell.Current.GoToAsync("//HomePage");
        }

        [RelayCommand]
        private async Task Login()
            => await Shell.Current.GoToAsync($"{nameof(LoginPage)}");

        [RelayCommand]
        private async Task DeleteAccount()
        {
            var confirmed = await Shell.Current.DisplayAlert(
                "Delete Account", "This cannot be undone. Continue?", "Yes", "No");
            if (!confirmed) return;

            var password = await Shell.Current.DisplayPromptAsync(
                "Confirm Password", "Enter your password:");
            if (string.IsNullOrEmpty(password)) return;

            var reAuth = await _firebaseAuth.ReAuthenticateAsync(
                _firebaseAuth.CurrentUserEmail, password);

            if (!reAuth)
            {
                await Shell.Current.DisplayAlert("Error", "Wrong password.", "OK");
                return;
            }

            var uid = await SecureStorage.GetAsync("firebaseUid");
            await _firebaseUserService.DeleteUserProfile(uid);

            var deleted = await _firebaseAuth.DeleteAccountAsync();
            if (!deleted)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete account.", "OK");
                return;
            }

            await _firebaseAuth.LogoutAsync();
            await Shell.Current.DisplayAlert("Deleted", "Account permanently removed.", "OK");
            await Shell.Current.GoToAsync("//HomePage");
        }

        [RelayCommand]
        private async Task SaveProfile()
        {
            if (User == null)
            {
                await Shell.Current.DisplayAlert("Error", "Profile data is missing.", "OK");
                return;
            }

            var uid = await SecureStorage.GetAsync("firebaseUid");
            if (string.IsNullOrEmpty(uid)) return;

            var profileData = new Dictionary<string, object>
            {
                { "fullName", User.FullName ?? string.Empty },
                { "phone",    User.Phone    ?? string.Empty },
                { "street",   User.Street   ?? string.Empty },
                { "city",     User.City     ?? string.Empty },
                { "province", User.Province ?? string.Empty },
            };

            await _firebaseUserService.SaveUserProfile(uid, profileData);
            await Shell.Current.DisplayAlert("Success", "Profile updated!", "OK");
            await Shell.Current.GoToAsync("..");
        }


        [RelayCommand]
        private async Task ViewOrders()
    => await Shell.Current.GoToAsync(nameof(OrderHistoryPage));

        [RelayCommand]
        private async Task GoToEditProfile()
            => await Shell.Current.GoToAsync(nameof(EditProfilePage));

        // ── About ─────────────────────────────────────────────────────────────────

        // FIX: Was named OpenDeveloper() → generated OpenDeveloperCommand.
        // XAML binds {Binding DeveloperCommand} → renamed to Developer().
        [RelayCommand]
        private static async Task Developer()
            => await Browser.OpenAsync("https://www.facebook.com/davecarlubas31");

        // XAML binds {Binding OpenGithubCommand} → method OpenGithub() ✅
        [RelayCommand]
        private static async Task OpenGithub()
            => await Browser.OpenAsync("https://github.com/BraveCarl");

        // ── Helper ────────────────────────────────────────────────────────────────

        private static string GetStringOrFallback(
            Dictionary<string, object> data, string key, string fallback)
        {
            if (data == null) return fallback;
            if (!data.TryGetValue(key, out var val)) return fallback;
            return val?.ToString() ?? fallback;
        }
    }
}