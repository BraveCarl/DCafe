using DCafe.ViewModels;

namespace DCafe.Views;

/// <summary>
/// Code-behind for OrderHistoryPage.
/// All logic lives in OrderHistoryViewModel — nothing goes here.
/// </summary>
public partial class OrderHistoryPage : ContentPage
{
    public OrderHistoryPage(OrderHistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}