using CommunityToolkit.Maui;
using MauiStoreApp.Services;
using MauiStoreApp.ViewModels;
using MauiStoreApp.Views;
using Microsoft.Extensions.Logging;
//using Plugin.Maui.Audio;


namespace MauiStoreApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("DCafé-eczar-semi-bold.ttf", "DCafé-eczar-semi-bold");
                    // Keep the old alias so existing XAML that references the
                    // Astore font family continues to work after renaming.
                    fonts.AddFont("DCafé-eczar-semi-bold.ttf", "AstoreEczarSemiBold");
                })
                .UseMauiCommunityToolkit();

            // Firebase — manually register the IFirebaseAuth instance for DI.
            // UseFirebaseAuth() does not exist in Plugin.Firebase 5.x so we
            // register CrossFirebaseAuth.Current (which is still available) directly.
        

            // Services
            builder.Services.AddSingleton<BaseService>();
            builder.Services.AddSingleton<ProductService>();
            builder.Services.AddSingleton<CategoryService>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<RecentlyViewedProductsService>();
            builder.Services.AddSingleton<FirebaseAuthService>(); // IFirebaseAuth injected above
            builder.Services.AddSingleton<PendingCartService>();
            builder.Services.AddSingleton<CartService>();    // already exists — keep it
            builder.Services.AddSingleton<OrderService>();
            builder.Services.AddSingleton<FirebaseUserService>();
            builder.Services.AddSingleton<CourierService>();
             

            // View models
            builder.Services.AddTransient<HomePageViewModel>();
            builder.Services.AddTransient<ProductDetailsViewModel>();
            builder.Services.AddTransient<CategoryPageViewModel>();
            builder.Services.AddTransient<RecentlyViewedPageViewModel>();
            builder.Services.AddTransient<CartViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<ProfilePageViewModel>();
            builder.Services.AddTransient<CheckoutViewModel>();
            builder.Services.AddTransient<SuccessViewModel>();
            builder.Services.AddTransient<OrderHistoryViewModel>();
            builder.Services.AddTransient<EditProfileViewModel>();
            builder.Services.AddTransient<CourierDashboardViewModel>();
            

            // Pages
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<ProductDetailsPage>();
            builder.Services.AddTransient<CategoryPage>();
            builder.Services.AddTransient<RecentlyViewedPage>();
            builder.Services.AddTransient<CartPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<CheckoutPage>();
            builder.Services.AddTransient<SuccessPage>();
            builder.Services.AddTransient<OrderHistoryPage>();
            builder.Services.AddTransient<EditProfilePage>();
            builder.Services.AddTransient<CourierOrderListPage>();
            builder.Services.AddTransient<CourierDashboardPage>();
           


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}