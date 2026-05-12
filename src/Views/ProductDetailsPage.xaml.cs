using DCafe.ViewModels;

namespace DCafe.Views;

public partial class ProductDetailsPage : ContentPage
{
	public ProductDetailsPage(ProductDetailsViewModel productDetailsViewModel)
	{
		InitializeComponent();
		BindingContext = productDetailsViewModel;
	}
}