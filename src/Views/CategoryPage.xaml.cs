using DCafe.ViewModels;

namespace DCafe.Views;

public partial class CategoryPage : ContentPage
{
	public CategoryPage(CategoryPageViewModel categoryPageViewModel)
	{
		InitializeComponent();
		BindingContext = categoryPageViewModel;
	}
}