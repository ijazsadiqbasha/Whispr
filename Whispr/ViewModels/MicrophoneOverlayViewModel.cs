using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace Whispr.ViewModels
{
    public class MicrophoneOverlayViewModel : ReactiveObject
    {
        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public MicrophoneOverlayViewModel()
        {
            _isVisible = false;
        }

        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
        }
    }
}