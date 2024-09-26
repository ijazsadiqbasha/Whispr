using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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