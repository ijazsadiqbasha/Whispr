using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Whispr.Services;
using Whispr.Models;  // Add this line to import AppSettings

namespace Whispr.ViewModels
{
    public class MicrophoneOverlayViewModel : ReactiveObject
    {
        private readonly IAudioCaptureService _audioCaptureService;
        private readonly IWhisperModelService _whisperModelService;
        private readonly AppSettings _appSettings;
        private bool _isVisible;
        private byte[] _audioData;
        private bool _isMicrophoneInitialized;
        private float _audioLevel;
        private bool _isModelLoaded = false;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                this.RaiseAndSetIfChanged(ref _isVisible, value);
                HandleVisibilityChange(value);
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

        public MicrophoneOverlayViewModel(IAudioCaptureService audioCaptureService, IWhisperModelService whisperModelService, AppSettings appSettings)
        {
            _audioCaptureService = audioCaptureService;
            _whisperModelService = whisperModelService;
            _appSettings = appSettings;
            _isVisible = false;
            _audioData = Array.Empty<byte>();
            _isMicrophoneInitialized = false;

            _audioCaptureService.AudioDataCaptured += OnAudioDataCaptured;
            _audioCaptureService.AudioLevelChanged += OnAudioLevelChanged;
            InitializeMicrophoneAsync();
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

        private async void HandleVisibilityChange(bool isVisible)
        {
            if (isVisible)
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
                    string transcription = await _whisperModelService.TranscribeAsync(_audioData);
                    Debug.WriteLine($"Transcription: {transcription}");
                }
                catch (InvalidOperationException)
                {
                    Debug.WriteLine("Model is not loaded. Please load the model before transcribing.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Transcription failed: {ex.Message}");
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
        }

        private void OnAudioDataCaptured(object sender, byte[] e)
        {
            _audioData = e;
            Debug.WriteLine($"Captured {e.Length} bytes of audio data");
        }

        private void OnAudioLevelChanged(object? sender, float e)
        {
            AudioLevel = e;
        }

        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
            Debug.WriteLine($"Visibility toggled: {IsVisible}");
        }

        public async Task DownloadModelAsync()
        {
            try
            {
                await _whisperModelService.DownloadModelAsync(_appSettings.AIModel);
                Debug.WriteLine("Whisper model downloaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to download Whisper model: {ex.Message}");
            }
        }

        public async Task LoadModelAsync()
        {
            try
            {
                bool loaded = await _whisperModelService.LoadModelAsync(_appSettings.AIModel);
                IsModelLoaded = loaded;
                Debug.WriteLine($"Whisper model loaded successfully: {loaded}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Whisper model: {ex.Message}");
                IsModelLoaded = false;
            }
        }
    }
}