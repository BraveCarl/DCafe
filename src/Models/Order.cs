using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiStoreApp.Models
{
    public class Order : INotifyPropertyChanged
    {
        // ── Identity ──────────────────────────────────────────────────────────────

        public string OrderId { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpper();
        public string UserId { get; set; }
        public DateTime PlacedAt { get; set; } = DateTime.Now;

        // ── Delivery ──────────────────────────────────────────────────────────────

        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        // ── Courier (NEW) ─────────────────────────────────────────────────────────

        private string _courierUid;
        /// <summary>Firebase UID of the assigned courier. Null = not yet assigned.</summary>
        public string CourierUid
        {
            get => _courierUid;
            set
            {
                if (_courierUid != value)
                {
                    _courierUid = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAssigned));
                    OnPropertyChanged(nameof(CourierDisplay));
                }
            }
        }

        private string _courierName;
        public string CourierName
        {
            get => _courierName;
            set
            {
                if (_courierName != value) { _courierName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CourierDisplay)); }
            }
        }

        /// <summary>True when a courier has accepted/been assigned this order.</summary>
        public bool IsAssigned => !string.IsNullOrEmpty(CourierUid);

        /// <summary>Display string for the courier field in UI.</summary>
        public string CourierDisplay => string.IsNullOrEmpty(CourierName) ? "Unassigned" : CourierName;

        // ── Payment ───────────────────────────────────────────────────────────────

        public string PaymentMethod { get; set; } = "COD";
        public string ReferenceNumber { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public DateTime? ConfirmedAt { get; set; }

        private string _paymentStatus = "Unpaid";
        public string PaymentStatus
        {
            get => _paymentStatus;
            set
            {
                if (_paymentStatus != value)
                {
                    _paymentStatus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDelivered));
                    OnPropertyChanged(nameof(PaymentStatusColor));
                }
            }
        }

        // ── Status ────────────────────────────────────────────────────────────────

        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDelivered));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(CanRate));
                    // Courier computed properties
                    OnPropertyChanged(nameof(IsAcceptedByCourier));
                    OnPropertyChanged(nameof(CanCourierAdvance));
                    OnPropertyChanged(nameof(CourierActionLabel));
                    OnPropertyChanged(nameof(CourierNextStatus));
                    OnPropertyChanged(nameof(IsAvailableForPickup));
                }
            }
        }

        // ── Rating ────────────────────────────────────────────────────────────────

        private bool _isRated;
        public bool IsRated
        {
            get => _isRated;
            set
            {
                if (_isRated != value)
                {
                    _isRated = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanRate));
                    OnPropertyChanged(nameof(StarDisplay));
                }
            }
        }

        private int _rating;
        public int Rating
        {
            get => _rating;
            set { if (_rating != value) { _rating = value; OnPropertyChanged(); OnPropertyChanged(nameof(StarDisplay)); } }
        }

        private string _review;
        public string Review
        {
            get => _review;
            set { if (_review != value) { _review = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasReview)); } }
        }

        // ── Customer computed helpers ─────────────────────────────────────────────

        public bool IsDelivered => Status == "Delivered";
        public bool IsAcceptedByCourier =>
    !string.IsNullOrEmpty(CourierUid) && Status != "Pending";
        public bool CanRate => Status == "Completed" && !IsRated;
        public bool HasReview => !string.IsNullOrWhiteSpace(Review);

        public string StarDisplay =>
            Rating > 0 ? new string('★', Rating) + new string('☆', 5 - Rating) : string.Empty;

        // ── Courier computed helpers (NEW) ────────────────────────────────────────

        /// <summary>
        /// True when the order is Pending and has no courier yet.
        /// Shows the "Accept Order" button on the courier's Available tab.
        /// </summary>
        public bool IsAvailableForPickup => Status == "Pending" && !IsAssigned;

        /// <summary>
        /// True when a courier can push this order to the next status.
        /// Visible on the courier's My Deliveries tab.
        /// </summary>
        public bool CanCourierAdvance =>
           Status is "Accepted by Courier" or "Processing" or "Shipped" or "Out for Delivery";

        /// <summary>The next logical status string, used internally by CourierService.</summary>
        public string CourierNextStatus => Status switch
        {
            "Accepted by Courier" => "Processing",        // ← ADD
            "Processing" => "Shipped",
            "Shipped" => "Out for Delivery",
            "Out for Delivery" => "Delivered",
            _ => null
        };

        /// <summary>Button label shown to courier, e.g. "Mark as Shipped".</summary>
        public string CourierActionLabel => Status switch
        {
            "Accepted by Courier" => "⚙️  Start Processing",
            "Processing" => "📦  Mark as Shipped",
            "Shipped" => "🚚  Out for Delivery",

            // Distinct label — signals this is the final delivery confirmation, not just a status push.
            "Out for Delivery" => "✅  Confirm Delivery to Customer",
            _ => string.Empty
        };

        // ── Color helpers ─────────────────────────────────────────────────────────

        public string StatusColor => Status switch
        {
            "Pending" => "#C9A96E",   // gold
            "Processing" => "#E0913A",   // amber
            "Shipped" => "#9B59B6",   // purple
            "Out for Delivery" => "#3498DB",   // blue
            "Delivered" => "#4A90E2",   // blue-lighter
            "Completed" => "#5DBB7A",   // green
                                        // In the StatusColor switch, ADD this line:
            "Accepted by Courier" => "#27AE60",   // green — courier is on it
            "For Verification" => "#E0913A",   // amber
            _ => "#7A6650",
        };

        public string PaymentStatusColor => PaymentStatus == "Paid" ? "#5DBB7A" : "#E57373";

        // ── Items ─────────────────────────────────────────────────────────────────

        public List<OrderItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }

        // ── INotifyPropertyChanged ────────────────────────────────────────────────

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}