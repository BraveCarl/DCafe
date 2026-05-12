using DCafe.ViewModels;

namespace DCafe.Views;

public partial class RecentlyViewedPage : ContentPage
{
	public RecentlyViewedPage(RecentlyViewedPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}