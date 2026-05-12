namespace MauiStoreApp.Models
{
    public class FirebaseUser
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Province { get; set; }

        /// <summary>
        /// "customer" (default) or "courier".
        /// Saved to Firebase at /users/{uid}/role during registration.
        /// Loaded in ProfilePageViewModel.Init() and checked in App.xaml.cs
        /// to decide which shell/tab bar to show.
        /// </summary>
        public string Role { get; set; } = "customer";

        public bool IsCourier => Role == "courier";

        public string AvatarInitials =>
            !string.IsNullOrWhiteSpace(FullName) ? FullName.Trim()[0].ToString().ToUpper() : "U";
    }
}