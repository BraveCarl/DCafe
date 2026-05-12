using MauiStoreApp.ViewModels;

namespace MauiStoreApp.Views;

public partial class CourierOrderListPage : ContentPage
{
    public CourierOrderListPage(CourierDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}