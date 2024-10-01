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
using Python.Runtime;
using Avalonia.Platform;
using System.Threading.Tasks;

namespace Whispr
{
    public partial class App : Application, IDisposable
    {
        private AppSettings? _appSettings;
        private TrayIcon? _trayIcon;
        private Settings? _settings;
        private IHotkeyService? _hotkeyService;
        private MicrophoneOverlay? _microphoneOverlay;
        private DateTime _recordingStartTime;
        private ServiceProvider? _serviceProvider;
        private bool _isRecording = false;

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
                _hotkeyService.HotkeyReleased += OnHotkeyReleased;

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
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_appSettings?.IsPythonInstalled == false)
                {
                    _settings?.Show();
                }
                else if (!_isRecording)
                {
                    _isRecording = true;
                    if (_microphoneOverlay?.DataContext is MicrophoneOverlayViewModel viewModel)
                    {
                        _recordingStartTime = DateTime.Now;
                        ToggleMicrophoneOverlay(viewModel);
                    }
                }
                else if (_appSettings?.RecordingMode == "Toggle with hotkey")
                {
                    _isRecording = false;
                    if (_microphoneOverlay?.DataContext is MicrophoneOverlayViewModel viewModel)
                    {
                        ToggleMicrophoneOverlay(viewModel);
                    }
                }
            });
        }

        private void OnHotkeyReleased(object? sender, EventArgs e)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_isRecording && _appSettings?.RecordingMode == "Press and hold")
                {
                    if (_microphoneOverlay?.DataContext is MicrophoneOverlayViewModel viewModel)
                    {
                        var elapsedTime = DateTime.Now - _recordingStartTime;
                        if (elapsedTime.TotalSeconds < 1)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1) - elapsedTime);
                        }
                       ToggleMicrophoneOverlay(viewModel);
                    }
                }
                _isRecording = false;
            });
        }

        private void ToggleMicrophoneOverlay(MicrophoneOverlayViewModel viewModel)
        {
            viewModel.ToggleVisibility();
            viewModel.ToggleRecording();
            if (viewModel.IsVisible)
            {
                _microphoneOverlay?.Show();
                viewModel.ProcessingCompleted += OnProcessingCompleted;
            }
        }

        private void OnProcessingCompleted(object? sender, string transcription)
        {
            try
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_microphoneOverlay?.DataContext is MicrophoneOverlayViewModel viewModel && !string.IsNullOrEmpty(transcription))
                    {
                        viewModel.ProcessingCompleted -= OnProcessingCompleted;
                        viewModel.IsVisible = false;
                        _microphoneOverlay?.Hide();

                        _hotkeyService?.SimulateTextInput(transcription);
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnProcessingCompleted: {ex.Message}");
            }
        }

        private ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _appSettings = AppSettings.LoadOrCreate();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<IHotkeyService, HotkeyService>();
            services.AddSingleton<IPythonInstallationService, PythonInstallationService>();
            services.AddSingleton<IWhisperModelService, WhisperModelService>();
            services.AddSingleton(_appSettings);
            services.AddTransient<PythonInstallationViewModel>();
            services.AddTransient<AppSettingsViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<MicrophoneOverlayViewModel>();
            services.AddSingleton<IAudioCaptureService, AudioCaptureService>();

            return services.BuildServiceProvider();
        }

        private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
        {
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Whispr/Assets/microphone.ico"))),
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
            if (_settings != null)
            {
                _settings.Show();
                _settings.Closing += (sender, args) =>
                {
                    args.Cancel = true;
                    _settings.Hide();
                };
            }
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