using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using Whispr.Models;
using Whispr.Services;
using Whispr.ViewModels;
using Whispr.Views;
using SharpHook;
using Avalonia.Metadata;
using SharpHook.Native;


namespace Whispr
{
    public partial class App : Application, IDisposable
    {
        private TrayIcon? _trayIcon;
        private Settings? _settings;
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
                SetupTrayIcon(desktop);

                _settings = new Settings
                {
                    DataContext = services.GetRequiredService<SettingsViewModel>()
                };

                _hotkeyService = services.GetRequiredService<IHotkeyService>();
                _hotkeyService.HotkeyTriggered += OnHotkeyTriggered;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnHotkeyTriggered(object? sender, EventArgs e)
        {
            // Handle hotkey trigger here
            Debug.WriteLine("Hotkey triggered!");
            // You can show your main window or perform any other action here
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var appSettings = AppSettings.LoadOrCreate();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IHotkeyService, HotkeyService>();
            services.AddSingleton<IPythonInstallationService, PythonInstallationService>();
            services.AddSingleton(appSettings);
            services.AddTransient<PythonInstallationViewModel>();
            services.AddTransient<AppSettingsViewModel>(sp =>
                new AppSettingsViewModel(sp.GetRequiredService<AppSettings>(), sp.GetRequiredService<IHotkeyService>()));
            services.AddTransient<SettingsViewModel>();

            return services.BuildServiceProvider();
        }

        private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
        {
            _trayIcon = new TrayIcon
            {
                //Icon = new WindowIcon("/Assets/avalonia-logo.ico"),
                ToolTipText = "SimpleDictation"
            };

            var openSettings = new NativeMenuItem("Open Settings");
            openSettings.Click += (sender, e) => ShowSettingsWindow();

            var quit = new NativeMenuItem("Quit");
            quit.Click += (sender, e) =>
            {
                Dispose();
                desktop.Shutdown();
                Environment.Exit(0);
            };

            _trayIcon.Menu = new NativeMenu
            {
                Items = { openSettings, quit }
            };
            _trayIcon.IsVisible = true;
        }

        private void ShowSettingsWindow()
        {
            _settings?.Show();
        }

        public void Dispose()
        {
            Debug.WriteLine("Disposing App resources");
            GC.SuppressFinalize(this);

            _trayIcon?.Dispose();
        }
    }
}