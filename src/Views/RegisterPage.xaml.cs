using MauiStoreApp.ViewModels;

namespace MauiStoreApp.Views;

public partial class RegisterPage : ContentPage
{
    // RegisterViewModel is now resolved from the DI container
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
