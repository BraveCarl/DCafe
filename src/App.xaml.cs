using System;
using MauiStoreApp.Services;

namespace MauiStoreApp
{
    public partial class App : Application
    {
        public App(AuthService authService, FirebaseAuthService firebaseAuthService)
        {
            InitializeComponent();

            // Global exception handlers for debugging crashes
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"[CRASH] UnhandledException: {ex?.Message}\n{ex?.StackTrace}");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[CRASH] UnobservedTaskException: {e.Exception?.Message}\n{e.Exception?.StackTrace}");
                e.SetObserved();
            };

            // ANR FIX: Initialize auth services in background to cache SecureStorage values.
            // This prevents repeated blocking SecureStorage calls during page navigation.
            _ = Task.Run(async () =>
            {
                try
                {
                    await authService.InitializeAsync();
                    await firebaseAuthService.InitializeAsync();
                    System.Diagnostics.Debug.WriteLine("[App] Auth services initialized");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[App] Auth init error: {ex.Message}");
                }
            });

            // Go directly to AppShell - native splash screen handles the launch screen
            MainPage = new AppShell();
        }
    }
}