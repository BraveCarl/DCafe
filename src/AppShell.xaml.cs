using DCafe.Views;


namespace DCafe
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(ProductDetailsPage), typeof(ProductDetailsPage));
            Routing.RegisterRoute(nameof(CategoryPage), typeof(CategoryPage));
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(CheckoutPage), typeof(CheckoutPage));
            Routing.RegisterRoute("CartPage", typeof(CartPage));
            Routing.RegisterRoute("SuccessPage", typeof(SuccessPage));
            Routing.RegisterRoute("OrderHistoryPage", typeof(OrderHistoryPage));
            Routing.RegisterRoute(nameof(EditProfilePage), typeof(EditProfilePage));
            Routing.RegisterRoute("CourierOrderListPage", typeof(CourierOrderListPage));
            Routing.RegisterRoute(nameof(CourierDashboardPage), typeof(CourierDashboardPage));

        }
    }
}