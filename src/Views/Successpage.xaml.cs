using MauiStoreApp.ViewModels;

namespace MauiStoreApp.Views;

public partial class SuccessPage : ContentPage
{
    public SuccessPage(SuccessViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}