using ReactiveUI;
using Whispr.Services;

namespace Whispr.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public PythonInstallationViewModel PythonInstallationViewModel { get; }
        public AppSettingsViewModel AppSettingsViewModel { get; }

        public SettingsViewModel(IPythonInstallationService pythonInstallationService)
        {
            PythonInstallationViewModel = new PythonInstallationViewModel(pythonInstallationService);
            AppSettingsViewModel = new AppSettingsViewModel();
        }
    }
}