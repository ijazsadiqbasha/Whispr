using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Whispr.Views
{
    public partial class MicrophoneOverlay : Window
    {
        public MicrophoneOverlay()
        {
            AvaloniaXamlLoader.Load(this);

            var workingArea = Screens.Primary?.WorkingArea;

            var windowWidth = this.Width;
            var centerX = workingArea?.X + (workingArea?.Width - windowWidth) / 2;

            var windowHeight = this.Height;
            var offsetFromTaskbar = 60;
            var bottomY = workingArea?.Y + workingArea?.Height - windowHeight - offsetFromTaskbar;

            this.Position = new PixelPoint((int)centerX.GetValueOrDefault(), (int)bottomY.GetValueOrDefault());
        }
    }
}