using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using Whispr.Models;
using Whispr.Services;

namespace Whispr.ViewModels
{
    public class PythonInstallationViewModel : ViewModelBase
    {
        private readonly IPythonInstallationService _pythonInstallationService;
        private readonly IWhisperModelService _whisperModelService;

        private string _pythonStatusText = string.Empty;
        private bool _isPythonProgressVisible = false;
        private double _progressValue = 0;
        private bool _isDownloadEnabled = true;

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

        public ReactiveCommand<Unit, Unit> DownloadPythonCommand { get; }

        public PythonInstallationViewModel(IPythonInstallationService pythonInstallationService, IWhisperModelService whisperModelService, AppSettings settings)
            : base(settings)
        {
            _pythonInstallationService = pythonInstallationService;
            _whisperModelService = whisperModelService;
            DownloadPythonCommand = ReactiveCommand.CreateFromTask(DownloadPython);
            UpdateUIBasedOnSettings();
        }

        private void UpdateUIBasedOnSettings()
        {
            if (Settings.IsPythonInstalled)
            {
                PythonStatusText = "Python is installed and verified.";
                IsDownloadEnabled = false;
            }
        }

        private async Task DownloadPython()
        {
            await ExecuteWithProgressBar(async () =>
            {
                if (_whisperModelService is IDisposable disposable)
                {
                    disposable.Dispose();
                }

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

        private async Task ExecuteWithProgressBar(Func<Task> action)
        {
            IsDownloadEnabled = false;
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
            Settings.IsPythonInstalled = true;
            Settings.PythonPath = _pythonInstallationService.GetPythonPath();
            SaveSettings();
        }
    }
}