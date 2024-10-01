using Python.Runtime;
using ReactiveUI;
using SharpHook.Native;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Whispr.Models;
using Whispr.Services;

namespace Whispr.ViewModels
{
    public class AppSettingsViewModel : ViewModelBase
    {

        private string _modelStatusText = string.Empty;
        private bool _isModelProgressVisible = false;
        private double _modelProgress = 0;
        private string _selectedShortcutKey = string.Empty;
        private string _selectedAIModel = string.Empty;

        public string ModelStatusText
        {
            get => _modelStatusText;
            set => this.RaiseAndSetIfChanged(ref _modelStatusText, value);
        }

        public bool IsDownloadButtonEnabled
        {
            get => !IsModelDownloaded(SelectedAIModel);
        }

        public bool IsModelProgressVisible
        {
            get => _isModelProgressVisible;
            set => this.RaiseAndSetIfChanged(ref _isModelProgressVisible, value);
        }

        public double ModelProgressValue
        {
            get => _modelProgress;
            set => this.RaiseAndSetIfChanged(ref _modelProgress, value);
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
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedAIModel, value);
                ChangeAIModel();
            }
        }

        public ObservableCollection<string> ShortcutKeys { get; set; }
        public ObservableCollection<string> AIModels { get; set; }

        public ReactiveCommand<Unit, Unit> DownloadModelCommand { get; }
        public ReactiveCommand<Unit, Unit> LoadModelCommand { get; }

        private readonly IHotkeyService _hotkeyService;
        private readonly IWhisperModelService _whisperModelService;

        private AppSettings _appSettings;

        public AppSettingsViewModel(AppSettings settings, IHotkeyService hotkeyService, IWhisperModelService whisperModelService)
            : base(settings)
        {
            _appSettings = settings;
            _hotkeyService = hotkeyService;
            _whisperModelService = whisperModelService;

            ShortcutKeys = [
                "Space", "Tab",
                "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
                "NumPad0", "NumPad1", "NumPad2", "NumPad3", "NumPad4", "NumPad5", "NumPad6", "NumPad7", "NumPad8", "NumPad9",
                "Esc", "Insert", "Delete", "Home", "End", "PageUp", "PageDown"
            ];

            AIModels = [
                "openai/whisper-tiny",
                "openai/whisper-base",
                "openai/whisper-small",
                "openai/whisper-medium",
                "openai/whisper-large",
                "openai/whisper-large-v2",
                "distil-whisper/distil-small.en",
                "distil-whisper/distil-medium.en",
                "distil-whisper/distil-large-v2"
            ];

            DownloadModelCommand = ReactiveCommand.CreateFromTask(DownloadModel);

            LoadModelCommand = ReactiveCommand.CreateFromTask(LoadModel);

            // Set initial shortcut key based on settings
            _selectedShortcutKey = ConvertKeyCodeToShortcutKey(Settings.Hotkey);
            Debug.WriteLine($"Initial hotkey loaded from settings: {_selectedShortcutKey} (0x{Settings.Hotkey:X})");
            
            // Set initial AIModel based on settings
            _selectedAIModel = Settings.AIModel;
            Debug.WriteLine($"Initial AIModel loaded from settings: {_selectedAIModel}");

            ChangeHotkey();

            this.WhenAnyValue(x => x.SelectedAIModel)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsDownloadButtonEnabled)));
        }

        private bool IsModelDownloaded(string modelName)
        {
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "whisper_models", modelName.Replace('/', '_'));
            return Directory.Exists(modelPath);
        }

        private async Task DownloadModel()
        {
            IsModelProgressVisible = true;
            ModelProgressValue = 0;
            ModelStatusText = "Initializing...";

            try
            {
                ModelProgressValue = 0.33;
                ModelStatusText = "Downloading model...";
                await _whisperModelService.DownloadModelAsync(SelectedAIModel);

                ModelProgressValue = 100;
                ModelStatusText = "Model downloaded and converted successfully";
            }
            catch (Exception e)
            {
                string errorMessage = $"Error downloading or converting model: {e.Message}";
                Console.Error.WriteLine(errorMessage);
                ModelStatusText = errorMessage;
            }

            IsModelProgressVisible = false;
        }

        private async Task LoadModel()
        {
            ModelStatusText = "Loading model...";

            try
            {
                await _whisperModelService.LoadModelAsync(SelectedAIModel);
                ModelStatusText = "Model loaded and running.";
            }
            catch (Exception e)
            {
                string errorMessage = $"Error loading model: {e.Message}";
                Console.Error.WriteLine(errorMessage);
                ModelStatusText = errorMessage;
            }
        }

        private void ChangeHotkey()
        {
            try
            {
                int keyCode = ConvertShortcutKeyToKeyCode(SelectedShortcutKey);
                Debug.WriteLine($"Changing hotkey to: 0x{keyCode:X} ({SelectedShortcutKey})");
                if (_hotkeyService.ChangeKey(keyCode))
                {
                    Settings.Hotkey = keyCode;
                    SaveSettings();
                    Debug.WriteLine($"Hotkey changed successfully to: 0x{keyCode:X} ({SelectedShortcutKey})");
                }
                else
                {
                    // Revert to the previous key if change failed
                    SelectedShortcutKey = ConvertKeyCodeToShortcutKey(Settings.Hotkey);
                    ModelStatusText = "Failed to change hotkey. It might be in use by another application.";
                    Debug.WriteLine($"Failed to change hotkey to: 0x{keyCode:X} ({SelectedShortcutKey})");
                }
            }
            catch (Exception ex)
            {
                ModelStatusText = $"Failed to change hotkey: {ex.Message}";
                Debug.WriteLine($"Exception while changing hotkey: {ex}");
                // Revert to the previous key
                SelectedShortcutKey = ConvertKeyCodeToShortcutKey(Settings.Hotkey);
            }
        }

        private void ChangeAIModel()
        {
            Settings.AIModel = SelectedAIModel;
            SaveSettings();
            Debug.WriteLine("AI Model changed saved successfully");
        }

        private static int ConvertShortcutKeyToKeyCode(string shortcutKey)
        {
            return shortcutKey switch
            {
                "Space" => 0x20,
                "Tab" => 0x09,
                "Esc" => 0x1B,
                "Insert" => 0x2D,
                "Delete" => 0x2E,
                "Home" => 0x24,
                "End" => 0x23,
                "PageUp" => 0x21,
                "PageDown" => 0x22,
                var s when s.Length == 1 && char.IsLetter(s[0]) => s[0],
                var s when s.Length == 1 && char.IsDigit(s[0]) => s[0],
                var s when s.StartsWith('F') && int.TryParse(s[1..], out int fKey) && fKey >= 1 && fKey <= 12
                    => 0x70 + fKey - 1,
                var s when s.StartsWith("NumPad") && int.TryParse(s[6..], out int numPadKey) && numPadKey >= 0 && numPadKey <= 9
                    => 0x60 + numPadKey,
                _ => 0x20 // Default to space
            };
        }

        private static string ConvertKeyCodeToShortcutKey(int keyCode)
        {
            return keyCode switch
            {
                0x20 => "Space",
                0x09 => "Tab",
                0x1B => "Esc",
                0x2D => "Insert",
                0x2E => "Delete",
                0x24 => "Home",
                0x23 => "End",
                0x21 => "PageUp",
                0x22 => "PageDown",
                >= 0x41 and <= 0x5A => ((char)keyCode).ToString(),
                >= 0x30 and <= 0x39 => ((char)keyCode).ToString(),
                >= 0x70 and <= 0x7B => $"F{keyCode - 0x70 + 1}",
                >= 0x60 and <= 0x69 => $"NumPad{keyCode - 0x60}",
                _ => "Space" // Default to space
            };
        }
    }
}