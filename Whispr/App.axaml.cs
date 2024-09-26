using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Settings
                {
                    DataContext = new SettingsViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}