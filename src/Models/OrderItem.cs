namespace MauiStoreApp.Models
{
    /// <summary>
    /// Immutable snapshot of one cart line at checkout time.
    /// Stored separately from CartItemDetail so cart changes
    /// never mutate historical orders.
    ///
    /// FIX: The original OrderItem.cs was an empty class body.
    /// Order.cs defined the real OrderItem inline — causing CS0101
    /// (duplicate type in same namespace) at compile time.
    /// Now OrderItem lives here only; Order.cs no longer defines it.
    /// </summary>
    public class OrderItem
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public string ProductImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}