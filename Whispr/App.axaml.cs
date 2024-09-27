using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Threading.Tasks;
using Whispr.Services;
using Whispr.ViewModels;
using Whispr.Views;

namespace Whispr
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && Program.Services != null)
            {
                var pythonInstallationService = Program.Services.GetRequiredService<IPythonInstallationService>();

                desktop.MainWindow = new Settings
                {
                    DataContext = new SettingsViewModel(pythonInstallationService),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}