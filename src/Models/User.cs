using System.Text.Json.Serialization;

namespace MauiStoreApp.Models
{
    /// <summary>
    /// Represents a user.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets the user's identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's username.
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        [JsonPropertyName("password")]
        public string Password { get; set; }

        // NEW PROPERTY
        [JsonPropertyName("role")]
        public string Role { get; set; } = "customer";

        // NEW HELPERS
        public bool IsCustomer => Role == "customer" || string.IsNullOrEmpty(Role);

        public bool IsCourier => Role == "courier";

        /// <summary>
        /// Gets or sets the user's name information.
        /// </summary>
        [JsonPropertyName("name")]
        public Name Name { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number.
        /// </summary>
        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets the user's address information.
        /// </summary>
        [JsonPropertyName("address")]
        public Address Address { get; set; }

        /// <summary>
        /// Gets the user's full name.
        /// </summary>
        public string FullName => $"{Name?.Firstname?.ToUpper()[0]}{Name?.Firstname?.ToLower()[1..]} {Name?.Lastname?.ToUpper()[0]}{Name?.Lastname?.ToLower()[1..]}";

        /// <summary>
        /// Gets the initials of the user's name.
        /// </summary>
        public string AvatarInitials => $"{Name?.Firstname?.ToUpper()[0]}{Name?.Lastname?.ToUpper()[0]}";
    }
}