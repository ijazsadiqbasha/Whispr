using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
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
using Python.Runtime;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Whispr
{
    public partial class App : Application, IDisposable
    {
        private TrayIcon? _trayIcon;
        private Settings? _settings;
        private IHotkeyService? _hotkeyService;
        private MicrophoneOverlay? _microphoneOverlay;
        private ServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _serviceProvider = ConfigureServices();

                desktop.Exit += OnExit;

                _settings = new Settings
                {
                    DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>()
                };

                _hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
                _hotkeyService.HotkeyTriggered += OnHotkeyTriggered;

                _microphoneOverlay = new MicrophoneOverlay
                {
                    DataContext = _serviceProvider.GetRequiredService<MicrophoneOverlayViewModel>()
                };

                SetupTrayIcon(desktop);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnHotkeyTriggered(object? sender, EventArgs e)
        {
            Debug.WriteLine("Hotkey triggered!");

            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_microphoneOverlay?.DataContext is MicrophoneOverlayViewModel viewModel)
                {
                    viewModel.ToggleVisibility();
                    if (viewModel.IsVisible)
                    {
                        _microphoneOverlay.Show();
                    }
                    else
                    {
                        _microphoneOverlay.Hide();
                    }
                }
            });
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
            services.AddSingleton<IWhisperModelService, WhisperModelService>();
            services.AddSingleton(appSettings);
            services.AddTransient<PythonInstallationViewModel>();
            services.AddTransient<AppSettingsViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<MicrophoneOverlayViewModel>();
            services.AddSingleton<IAudioCaptureService, NAudioCaptureService>();

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

            try
            {
                PythonEngine.Shutdown();
            }
            catch (NotSupportedException ex)
            {
                Debug.WriteLine($"PythonEngine shutdown failed: {ex.Message}");
            }
        }

        private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            if (_serviceProvider != null)
            {
                var whisperModelService = _serviceProvider.GetService<IWhisperModelService>();
                if (whisperModelService is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _serviceProvider.Dispose();
            }
        }
    }
}