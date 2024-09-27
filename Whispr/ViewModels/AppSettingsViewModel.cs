using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Whispr.Models;
using Whispr.Services.Interfaces;
using Whispr.Views;

namespace Whispr.ViewModels
{
    public class AppSettingsViewModel : ViewModelBase
    {
        private readonly IHotkeyService _hotkeyService;

        private string _modelStatusText = string.Empty;
        private bool _isModelProgressVisible = false;
        private string _selectedShortcutKey = "Space";
        private string _selectedAIModel = "Model1";

        public string ModelStatusText
        {
            get => _modelStatusText;
            set => this.RaiseAndSetIfChanged(ref _modelStatusText, value);
        }

        public bool IsModelProgressVisible
        {
            get => _isModelProgressVisible;
            set => this.RaiseAndSetIfChanged(ref _isModelProgressVisible, value);
        }

        public string SelectedShortcutKey
        {
            get => _selectedShortcutKey;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedShortcutKey, value);
                ChangeHotkey();
            }
        }

        public string SelectedAIModel
        {
            get => _selectedAIModel;
            set => this.RaiseAndSetIfChanged(ref _selectedAIModel, value);
        }

        public ObservableCollection<string> ShortcutKeys { get; set; }
        public ObservableCollection<string> AIModels { get; set; }

        public ReactiveCommand<Unit, Unit> DownloadModelCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadModelCommand { get; }

        public AppSettingsViewModel(IHotkeyService hotkeyService, AppSettings settings)
            : base(settings)
        {
            _hotkeyService = hotkeyService;

            ShortcutKeys = ["Space", "Enter", "Tab"];
            AIModels = ["Model1", "Model2", "Model3"];

            DownloadModelCommand = ReactiveCommand.CreateFromTask(DownloadModel);
            LoadModelCommand = ReactiveCommand.CreateFromTask(LoadModel);

            // Set initial shortcut key based on settings
            SelectedShortcutKey = ConvertKeyCodeToShortcutKey(Settings.Hotkey);
        }

        private async Task DownloadModel()
        {
            IsModelProgressVisible = true;
            ModelStatusText = "Downloading model...";
            // Simulate download
            await Task.Delay(2000);
            ModelStatusText = "Model downloaded.";
            IsModelProgressVisible = false;
        }

        private async Task LoadModel()
        {
            IsModelProgressVisible = true;
            ModelStatusText = "Loading model...";
            // Simulate loading
            await Task.Delay(2000);
            ModelStatusText = "Model loaded.";
            IsModelProgressVisible = false;
        }

        private void ChangeHotkey()
        {
            try
            {
                int keyCode = ConvertShortcutKeyToKeyCode(SelectedShortcutKey);
                _hotkeyService.ChangeKey(keyCode);
                Settings.Hotkey = keyCode;
                SaveSettings();
            }
            catch (Exception ex)
            {
                // Handle the exception, e.g., show an error message to the user
                ModelStatusText = $"Failed to change hotkey: {ex.Message}";
            }
        }

        private static int ConvertShortcutKeyToKeyCode(string shortcutKey)
        {
            return shortcutKey switch
            {
                "Space" => 32,
                "Enter" => 13,
                "Tab" => 9,
                _ => 32 // Default to space
            };
        }

        private static string ConvertKeyCodeToShortcutKey(int keyCode)
        {
            return keyCode switch
            {
                32 => "Space",
                13 => "Enter",
                9 => "Tab",
                _ => "Space" // Default to space
            };
        }
    }
}