using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using Whispr.Models;
using Whispr.Services;
using Whispr.Services.Interfaces;
using Whispr.ViewModels;
using Whispr.Views;

namespace Whispr
{
    public partial class App : Application
    {
        private IHotkeyService? _hotkeyService;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = ConfigureServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _hotkeyService = services.GetRequiredService<IHotkeyService>();
                var mainWindow = new Settings
                {
                    DataContext = services.GetRequiredService<SettingsViewModel>()
                };
                desktop.MainWindow = mainWindow;

                try
                {
                    _hotkeyService.Initialize(mainWindow, OnHotkeyPressed);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to initialize hotkey service: {ex.Message}");
                    // Optionally, show an error message to the user
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnHotkeyPressed()
        {
            Debug.WriteLine("Hotkey pressed!");
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IHotkeyService>(sp => HotkeyServiceFactory.Create(sp.GetRequiredService<IConfiguration>()));
            services.AddSingleton<IPythonInstallationService, PythonInstallationService>();
            services.AddSingleton(AppSettings.LoadOrCreate());
            services.AddTransient<PythonInstallationViewModel>();
            services.AddTransient<AppSettingsViewModel>();
            services.AddTransient<SettingsViewModel>();

            return services.BuildServiceProvider();
        }
    }
}