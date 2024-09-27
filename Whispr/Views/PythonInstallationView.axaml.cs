using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Whispr.Views
{
    public partial class PythonInstallationView : UserControl
    {
        public PythonInstallationView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}