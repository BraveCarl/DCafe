using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStoreApp.Services;

namespace MauiStoreApp.ViewModels
{
    public partial class RegisterViewModel : BaseViewModel
    {
        private readonly FirebaseAuthService _firebaseAuth;
        private readonly FirebaseUserService _firebaseUserService;   // NEW

        public RegisterViewModel(
            FirebaseAuthService firebaseAuth,
            FirebaseUserService firebaseUserService)                   // NEW
        {
            _firebaseAuth = firebaseAuth;
            _firebaseUserService = firebaseUserService;
        }

        public RegisterViewModel() { }

        // ── Existing fields (unchanged) ───────────────────────────────────────────

        [ObservableProperty] string username;
        [ObservableProperty] string email;
        [ObservableProperty] string password;
        [ObservableProperty] string confirmPassword;

        // ── NEW: Role selection ───────────────────────────────────────────────────

        // Source list for the Picker — shown as "Customer" / "Courier" in the UI
        public List<string> Roles { get; } = new() { "Customer", "Courier" };

        // Bound to Picker.SelectedItem — defaults to "Customer"
        [ObservableProperty]
        string selectedRole = "Customer";

        // ── Register command ──────────────────────────────────────────────────────

        [RelayCommand]
        private async Task Register()
        {
            if (IsBusy) return;

            // Validation (unchanged)
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Missing fields", "Please fill in all fields.", "OK");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Shell.Current.DisplayAlert("Error", "Passwords do not match.", "OK");
                return;
            }

            if (Password.Length < 6)
            {
                await Shell.Current.DisplayAlert("Weak password",
                    "Password must be at least 6 characters.", "OK");
                return;
            }

            // NEW: validate role
            if (string.IsNullOrWhiteSpace(SelectedRole))
            {
                await Shell.Current.DisplayAlert("Missing role",
                    "Please select Customer or Courier.", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                // Step 1: create Firebase Auth account (unchanged)
                var (success, error) = await _firebaseAuth.RegisterAsync(
                    Username.Trim(), Email.Trim(), Password);

                if (!success)
                {
                    await Shell.Current.DisplayAlert("Registration failed", error, "OK");
                    return;
                }

                // Step 2: NEW — save role to Firebase Realtime Database
                // /users/{uid}/role = "customer" or "courier"
                // Stored in lowercase to match the convention in User.cs / FirebaseUser.cs
                var uid = await SecureStorage.GetAsync("firebaseUid");

                if (!string.IsNullOrEmpty(uid))
                {
                    var profileData = new Dictionary<string, object>
                    {
                        { "fullName", Username.Trim() },
                        { "role",     SelectedRole.ToLower() },   // "customer" or "courier"
                    };

                    await _firebaseUserService.SaveUserProfile(uid, profileData);
                }

                await Shell.Current.DisplayAlert(
                    "Account created!",
                    $"Welcome, {Username}! You are registered as a {SelectedRole}.",
                    "Continue");

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task BackToLogin()
            => await Shell.Current.GoToAsync("..");
    }
}