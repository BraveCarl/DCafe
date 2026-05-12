using DCafe.ViewModels;

namespace DCafe.Views;

public partial class SuccessPage : ContentPage
{
    public SuccessPage(SuccessViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}