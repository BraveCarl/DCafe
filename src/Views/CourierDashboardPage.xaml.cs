using MauiStoreApp.ViewModels;

namespace MauiStoreApp.Views;

/// <summary>
/// Code-behind for CourierDashboardPage.
/// All logic lives in CourierDashboardViewModel — nothing goes here.
/// </summary>
public partial class CourierDashboardPage : ContentPage
{
    public CourierDashboardPage(CourierDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}