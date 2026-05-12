using DCafe.ViewModels;

namespace DCafe.Views;

public partial class CourierOrderListPage : ContentPage
{
    public CourierOrderListPage(CourierDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}