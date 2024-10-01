using Whispr.Models;

namespace Whispr.ViewModels
{
    public class SettingsViewModel(
        PythonInstallationViewModel pythonInstallationViewModel,
        AppSettingsViewModel appSettingsViewModel,
        AppSettings settings) : ViewModelBase(settings)
    {
        public PythonInstallationViewModel PythonInstallationViewModel { get; } = pythonInstallationViewModel;
        public AppSettingsViewModel AppSettingsViewModel { get; } = appSettingsViewModel;
    }
}