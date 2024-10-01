using ReactiveUI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Whispr.Services;
using Whispr.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using System.Collections.Generic;
using Avalonia.Media; // Use Avalonia.Media instead of System.Windows.Media
using System.Timers;

namespace Whispr.ViewModels
{
    public class MicrophoneOverlayViewModel : ReactiveObject
    {
        private readonly IAudioCaptureService _audioCaptureService;
        private readonly IWhisperModelService _whisperModelService;
        private readonly AppSettings _appSettings;
        private readonly IHotkeyService _hotkeyService;
        private bool _isVisible;
        private bool _isRecording;
        private byte[] _audioData;
        private bool _isMicrophoneInitialized;
        private float _audioLevel;
        private bool _isModelLoaded = false;
        private ObservableCollection<AudioBar> _audioBars;
        private float _maxAudioLevel = 0.1f; // Initialize with a small value
        private const float AUDIO_LEVEL_DECAY_FACTOR = 0.999f; // Decay factor for max audio level
        private const int BAR_COUNT = 30; // Increased from 10 to 30
        private bool _isProcessing;
        private double _progressAngle;
        private double _audioBarsOpacity = 1;
        private double _progressCircleOpacity = 0;
        private string _progressColor = "#FFFFFF";

        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                this.RaiseAndSetIfChanged(ref _isRecording, value);
                HandleRecordingChange(value);
            }
        }

        public float AudioLevel
        {
            get => _audioLevel;
            set => this.RaiseAndSetIfChanged(ref _audioLevel, value);
        }

        public bool IsModelLoaded
        {
            get => _isModelLoaded;
            set => this.RaiseAndSetIfChanged(ref _isModelLoaded, value);
        }

        public ObservableCollection<AudioBar> AudioBars
        {
            get => _audioBars;
            set => this.RaiseAndSetIfChanged(ref _audioBars, value);
        }

        public string ProgressColor
        {
            get => _progressColor;
            set => this.RaiseAndSetIfChanged(ref _progressColor, value);
        }

        public MicrophoneOverlayViewModel(IAudioCaptureService audioCaptureService, IWhisperModelService whisperModelService, AppSettings appSettings, IHotkeyService hotkeyService)
        {
            _audioCaptureService = audioCaptureService;
            _whisperModelService = whisperModelService;
            _appSettings = appSettings;
            _hotkeyService = hotkeyService;
            _isVisible = false;
            _audioData = Array.Empty<byte>();
            _isMicrophoneInitialized = false;

            _audioCaptureService.AudioDataCaptured += OnAudioDataCaptured;
            _audioCaptureService.AudioLevelChanged += OnAudioLevelChanged;
            InitializeMicrophoneAsync();
            AudioBars = new ObservableCollection<AudioBar>(Enumerable.Repeat(new AudioBar { Height = 1, Color = "White" }, BAR_COUNT));
        }

        private async void InitializeMicrophoneAsync()
        {
            _isMicrophoneInitialized = await _audioCaptureService.InitializeMicrophoneAsync();
            if (!_isMicrophoneInitialized)
            {
                Debug.WriteLine("Failed to initialize microphone");
            }
            else
            {
                Debug.WriteLine("Microphone initialized successfully");
            }
        }

        private async void HandleRecordingChange(bool isRecording)
        {
            if (isRecording)
            {
                await StartRecordingAsync();
            }
            else
            {
                await StopRecordingAndTranscribeAsync();
            }
        }

        private async Task StartRecordingAsync()
        {
            if (!_isMicrophoneInitialized)
            {
                Debug.WriteLine("Cannot start recording: Microphone not initialized");
                return;
            }
            _audioData = Array.Empty<byte>();
            await _audioCaptureService.StartCaptureAsync();
            Debug.WriteLine("Started recording");
        }

        private async Task StopRecordingAndTranscribeAsync()
        {
            if (!_isMicrophoneInitialized)
            {
                Debug.WriteLine("Cannot stop recording: Microphone not initialized");
                return;
            }
            await _audioCaptureService.StopCaptureAsync();
            Debug.WriteLine("Stopped recording");
            if (_audioData.Length > 0 && _whisperModelService.IsModelLoaded())
            {
                try
                {
                    IsProcessing = true;
                    UpdateProgressAngle(0); // Start progress at 0

                    string transcription = await _whisperModelService.TranscribeAsync(_audioData, progress =>
                    {
                        // This is our progress callback
                        UpdateProgressAngle(progress * 3.6); // Convert percentage to degrees (100% = 360 degrees)
                    });

                    UpdateProgressAngle(360); // Ensure we end at 100%
                    await Task.Delay(500); // Wait for animation to complete

                    _hotkeyService.SimulateTextInput(transcription);
                    Debug.WriteLine($"Transcription: {transcription}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Transcription failed: {ex.Message}");
                }
                finally
                {
                    IsProcessing = false;
                    await Task.Delay(100); // Short delay to ensure UI updates
                    // Remove the IsVisible = false line from here
                }
            }
            else if (!_whisperModelService.IsModelLoaded())
            {
                Debug.WriteLine("Model is not loaded. Cannot transcribe.");
            }
            else
            {
                Debug.WriteLine("No audio data to transcribe");
            }

            ProcessingCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void OnAudioDataCaptured(object? sender, byte[] e)
        {
            _audioData = e;
            Debug.WriteLine($"Captured {e.Length} bytes of audio data");
        }

        private void OnAudioLevelChanged(object? sender, float e)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AudioLevel = e;
                UpdateMaxAudioLevel(e);
                UpdateAudioBars(e);
            });
        }

        private void UpdateMaxAudioLevel(float currentLevel)
        {
            // Decay the max level over time
            _maxAudioLevel *= AUDIO_LEVEL_DECAY_FACTOR;

            // Update max level if current level is higher
            if (currentLevel > _maxAudioLevel)
            {
                _maxAudioLevel = currentLevel;
            }

            // Ensure _maxAudioLevel doesn't fall below a minimum threshold
            _maxAudioLevel = Math.Max(_maxAudioLevel, 0.01f);
        }

        private void UpdateAudioBars(float level)
        {
            const int maxBarHeight = 70; // 70% of 100 pixels
            const int minBarHeight = 5;  // Minimum height for visibility
            var random = new Random();
            var newBars = new List<AudioBar>();
            for (int i = 0; i < BAR_COUNT; i++)
            {
                // Scale the height relative to the max audio level and position
                double scaledLevel = level / _maxAudioLevel;
                double positionFactor = 1 - Math.Abs((i - (BAR_COUNT - 1) / 2.0) / (BAR_COUNT / 2.0));
                double height = Math.Max(minBarHeight,
                    minBarHeight + (maxBarHeight - minBarHeight) * scaledLevel * positionFactor * (0.5 + random.NextDouble()));
                string color = GetColorForBar(height, IsRecording);
                newBars.Add(new AudioBar { Height = height, Color = color });
            }

            // Update the collection on the UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AudioBars.Clear();
                foreach (var bar in newBars)
                {
                    AudioBars.Add(bar);
                }
            });
        }

        private string GetColorForBar(double height, bool isRecording)
        {
            if (!isRecording)
                return "White";

            // Calculate the intensity based on height (0 to 1)
            double intensity = Math.Min(height / 70.0, 1.0);

            // Start from white (255, 255, 255) and gradually introduce color
            byte r = (byte)(255);
            byte g = (byte)(255 - (155 * intensity)); // Decrease green to introduce more red
            byte b = (byte)(255 - (155 * intensity)); // Decrease blue to introduce more red

            return Color.FromRgb(r, g, b).ToString();
        }

        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
        }

        public void ToggleRecording()
        {
            IsRecording = !IsRecording;
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                this.RaiseAndSetIfChanged(ref _isProcessing, value);
                UpdateOpacities();
            }
        }

        public double ProgressAngle
        {
            get => _progressAngle;
            set
            {
                this.RaiseAndSetIfChanged(ref _progressAngle, value);
                UpdateProgressColor(value);
            }
        }

        public double AudioBarsOpacity
        {
            get => _audioBarsOpacity;
            set => this.RaiseAndSetIfChanged(ref _audioBarsOpacity, value);
        }

        public double ProgressCircleOpacity
        {
            get => _progressCircleOpacity;
            set => this.RaiseAndSetIfChanged(ref _progressCircleOpacity, value);
        }

        private void UpdateOpacities()
        {
            AudioBarsOpacity = IsProcessing ? 0 : 1;
            ProgressCircleOpacity = IsProcessing ? 1 : 0;
        }

        private void UpdateProgressAngle(double angle)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ProgressAngle = angle;
            });
        }

        private void UpdateProgressColor(double angle)
        {
            double progress = angle / 360.0;
            byte r = (byte)(255 * (1 - progress));
            byte g = (byte)(100 * progress);
            byte b = (byte)(0);
            ProgressColor = $"#{r:X2}{g:X2}{b:X2}";
        }

        // You may want to add a method to update progress during transcription
        // This would be called from your transcription service
        public void UpdateTranscriptionProgress(float progress)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                double angle = progress * 360;
                ProgressAngle = angle;
                UpdateProgressColor(angle);
            });
        }

        public event EventHandler? ProcessingCompleted;
    }

    public class AudioBar
    {
        public double Height { get; set; }
        public string Color { get; set; }
    }
}