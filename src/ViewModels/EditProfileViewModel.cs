using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiStoreApp.Services;

namespace MauiStoreApp.ViewModels
{
    public partial class EditProfileViewModel : BaseViewModel
    {
        private readonly FirebaseUserService _firebaseUserService;

        public EditProfileViewModel(FirebaseUserService firebaseUserService)
        {
            _firebaseUserService = firebaseUserService;
        }

        [ObservableProperty] string fullName;
        [ObservableProperty] string phone;
        [ObservableProperty] string street;
        [ObservableProperty] string city;
        [ObservableProperty] string province;

        // ── Load ──────────────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task LoadProfile()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var uid = await SecureStorage.GetAsync("firebaseUid");
                if (string.IsNullOrEmpty(uid)) return;

                var data = await _firebaseUserService.GetUserProfile(uid);

                FullName = SafeGet(data, "fullName");
                Phone = SafeGet(data, "phone");
                Street = SafeGet(data, "street");
                City = SafeGet(data, "city");
                Province = SafeGet(data, "province");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditProfileVM] LoadProfile: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Save ──────────────────────────────────────────────────────────────────

        [RelayCommand]
        public async Task SaveProfile()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var uid = await SecureStorage.GetAsync("firebaseUid");
                if (string.IsNullOrEmpty(uid))
                {
                    await Shell.Current.DisplayAlert("Error", "User not logged in.", "OK");
                    return;
                }

                var profileData = new Dictionary<string, object>
                {
                    { "fullName", FullName ?? string.Empty },
                    { "phone",    Phone    ?? string.Empty },
                    { "street",   Street   ?? string.Empty },
                    { "city",     City     ?? string.Empty },
                    { "province", Province ?? string.Empty },
                };

                await _firebaseUserService.SaveUserProfile(uid, profileData);

                if (!string.IsNullOrWhiteSpace(FullName))
                    await SecureStorage.Default.SetAsync("displayName", FullName.Trim());

                await Shell.Current.DisplayAlert("Success", "Profile updated!", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditProfileVM] SaveProfile: {ex.Message}");
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ── Back ──────────────────────────────────────────────────────────────────

        // FIX: Was named GoBackAsync() which generates GoBackAsyncCommand.
        // EditProfilePage.xaml binds {Binding GoBackCommand}.
        // Renamed to GoBack() so CommunityToolkit generates GoBackCommand. ✅
        [RelayCommand]
        private async Task GoBackAsync()
            => await Shell.Current.GoToAsync("..");

        // ── Helper ────────────────────────────────────────────────────────────────

        private static string SafeGet(Dictionary<string, object> data, string key)
        {
            if (data == null) return string.Empty;
            if (!data.TryGetValue(key, out var val)) return string.Empty;
            return val?.ToString() ?? string.Empty;
        }
    }
}