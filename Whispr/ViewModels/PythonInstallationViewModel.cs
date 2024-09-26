using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace Whispr.ViewModels
{
    public class PythonInstallationViewModel : ViewModelBase
    {
        private string _pythonStatusText = string.Empty;
        private bool _isPythonProgressVisible = false;

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

        public ReactiveCommand<Unit, Unit> DownloadPythonCommand { get; }
        public ReactiveCommand<Unit, Unit> VerifyPythonCommand { get; }

        public PythonInstallationViewModel()
        {
            DownloadPythonCommand = ReactiveCommand.CreateFromTask(DownloadPython);
            VerifyPythonCommand = ReactiveCommand.CreateFromTask(VerifyPython);
        }

        private async Task DownloadPython()
        {
            IsPythonProgressVisible = true;
            PythonStatusText = "Downloading Python...";
            // Simulate download
            await Task.Delay(2000);
            PythonStatusText = "Python downloaded.";
            IsPythonProgressVisible = false;
        }

        private async Task VerifyPython()
        {
            IsPythonProgressVisible = true;
            PythonStatusText = "Verifying Python...";
            // Simulate verification
            await Task.Delay(2000);
            PythonStatusText = "Python verified.";
            IsPythonProgressVisible = false;
        }
    }
}