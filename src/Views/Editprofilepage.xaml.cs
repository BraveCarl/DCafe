using MauiStoreApp.ViewModels;

namespace MauiStoreApp.Views;

public partial class EditProfilePage : ContentPage
{
    private readonly EditProfileViewModel _viewModel;

    public EditProfilePage(EditProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    // FIX: Use OnNavigatedTo instead of OnAppearing.
    // OnAppearing fires before Shell navigation is complete — if LoadProfile
    // internally triggers any navigation (e.g. redirect on auth failure) it
    // can race with the stack and crash.
    // OnNavigatedTo fires after the page is fully on the stack — safe for async.
    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await _viewModel.LoadProfileCommand.ExecuteAsync(null);
    }
}