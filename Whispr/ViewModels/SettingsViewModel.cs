using ReactiveUI;

namespace Whispr.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public PythonInstallationViewModel PythonInstallationViewModel { get; }
        public AppSettingsViewModel AppSettingsViewModel { get; }

        public SettingsViewModel()
        {
            PythonInstallationViewModel = new PythonInstallationViewModel();
            AppSettingsViewModel = new AppSettingsViewModel();
        }
    }
}