using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Whispr.ViewModels;

namespace Whispr.Views
{
    public partial class AppSettingsView : UserControl
    {
        public AppSettingsView()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}