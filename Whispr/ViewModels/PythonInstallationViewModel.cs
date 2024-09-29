using Python.Runtime;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Whispr.Models;
using Whispr.Services;

namespace Whispr.ViewModels
{
    public class PythonInstallationViewModel : ViewModelBase
    {
        private readonly IPythonInstallationService _pythonInstallationService;

        private string _pythonStatusText = string.Empty;
        private bool _isPythonProgressVisible = false;
        private double _progressValue = 0;
        private bool _isDownloadEnabled = true;
        private bool _isVerifyEnabled = true;

        public string PythonStatusText
        {
            get => _pythonStatusText;
            set => this.RaiseAndSetIfChanged(ref _pythonStatusText, value);
        }

        public bool IsPythonProgressVisible
        {
            get => _isPythonProgressVisible;
            set => this.RaiseAndSetIfChanged(ref _isPythonProgressVisible, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => this.RaiseAndSetIfChanged(ref _progressValue, value);
        }

        public bool IsDownloadEnabled
        {
            get => _isDownloadEnabled;
            set => this.RaiseAndSetIfChanged(ref _isDownloadEnabled, value);
        }

        public bool IsVerifyEnabled
        {
            get => _isVerifyEnabled;
            set => this.RaiseAndSetIfChanged(ref _isVerifyEnabled, value);
        }

        public ReactiveCommand<Unit, Unit> DownloadPythonCommand { get; }
        public ReactiveCommand<Unit, Unit> VerifyPythonCommand { get; }

        public PythonInstallationViewModel(IPythonInstallationService pythonInstallationService, AppSettings settings)
            : base(settings)
        {
            _pythonInstallationService = pythonInstallationService;
            DownloadPythonCommand = ReactiveCommand.CreateFromTask(DownloadPython);
            VerifyPythonCommand = ReactiveCommand.CreateFromTask(VerifyPython);
            UpdateUIBasedOnSettings();
        }

        private void UpdateUIBasedOnSettings()
        {
            if (Settings.IsPythonInstalled)
            {
                PythonStatusText = "Python is installed and verified.";
                IsDownloadEnabled = false;
                IsVerifyEnabled = false;

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string pythonHome = Path.Combine(baseDirectory, "python");
                string pythonDll = Path.Combine(pythonHome, "python311.dll");

                Debug.WriteLine($"Setting PYTHONNET_PYDLL to: {pythonDll}");
                Debug.WriteLine($"Setting PYTHONHOME to: {pythonHome}");

                Environment.SetEnvironmentVariable("PYTHONNET_PYDLL", pythonDll);
                Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);

                Runtime.PythonDLL = pythonDll;
                PythonEngine.Initialize();
                Debug.WriteLine("Python runtime initialized successfully.");
            }
        }

        private async Task DownloadPython()
        {
            await ExecuteWithProgressBar(async () =>
            {
                var isInstalled = await _pythonInstallationService.CheckPythonInstallationAsync();
                if (isInstalled)
                {
                    SetInstallationComplete("Python is already installed and verified.");
                    return;
                }

                await PerformInstallationSteps();
                await VerifyAndUpdateSettings();
            });
        }

        private async Task VerifyPython()
        {
            await ExecuteWithProgressBar(async () =>
            {
                var isPythonInstalled = await _pythonInstallationService.IsPythonInstalledAsync();
                if (!isPythonInstalled)
                {
                    PythonStatusText = "Python is not installed. Please download Python first.";
                    IsDownloadEnabled = true;
                    return;
                }

                await PerformInstallationSteps();
                await VerifyAndUpdateSettings();
            });
        }

        private async Task ExecuteWithProgressBar(Func<Task> action)
        {
            IsDownloadEnabled = false;
            IsVerifyEnabled = false;
            IsPythonProgressVisible = true;
            ProgressValue = 0;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                PythonStatusText = $"Error: {ex.Message}";
                IsDownloadEnabled = true;
            }
            finally
            {
                IsVerifyEnabled = true;
                IsPythonProgressVisible = false;
            }
        }

        private async Task PerformInstallationSteps()
        {
            PythonStatusText = "Downloading Python...";
            await _pythonInstallationService.DownloadPythonAsync(new Progress<double>(p => ProgressValue = p * 0.25));

            PythonStatusText = "Extracting Python...";
            await _pythonInstallationService.ExtractPythonAsync(new Progress<double>(p => ProgressValue = 0.25 + p * 0.25));

            PythonStatusText = "Installing Pip...";
            await _pythonInstallationService.SetupPipAsync(new Progress<double>(p => ProgressValue = 0.5 + p * 0.1));

            PythonStatusText = "Installing packages...";
            var packages = _pythonInstallationService.GetRequiredPackages();
            var packageProgressStep = 0.4 / packages.Length;
            await _pythonInstallationService.InstallPackagesAsync(
                new Progress<double>(p => ProgressValue = 0.6 + p * packageProgressStep),
                status => PythonStatusText = status
            );
        }

        private async Task VerifyAndUpdateSettings()
        {
            PythonStatusText = "Verifying installation...";
            var isVerified = await _pythonInstallationService.VerifyPythonInstallationAsync();
            ProgressValue = 1;

            if (isVerified)
            {
                SetInstallationComplete("Python installation completed and verified successfully.");
            }
            else
            {
                PythonStatusText = "Python installation failed verification.";
                IsDownloadEnabled = true;
            }
        }

        private void SetInstallationComplete(string message)
        {
            PythonStatusText = message;
            IsDownloadEnabled = false;
            IsVerifyEnabled = false;
            Settings.IsPythonInstalled = true;
            Settings.PythonPath = _pythonInstallationService.GetPythonPath();
            SaveSettings();
        }
    }
}