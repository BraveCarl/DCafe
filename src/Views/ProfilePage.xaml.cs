using MauiStoreApp.ViewModels;

namespace MauiStoreApp.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ProfilePageViewModel _viewModel;

    public ProfilePage(ProfilePageViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = _viewModel;
    }
}