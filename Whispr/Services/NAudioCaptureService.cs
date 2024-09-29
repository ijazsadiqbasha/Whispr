using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Whispr.Services
{
    public class NAudioCaptureService : IAudioCaptureService
    {
        private WaveInEvent? _waveIn;
        private bool _isCapturing;

        public event EventHandler<byte[]>? AudioDataCaptured;

        public bool IsCapturing => _isCapturing;

        public async Task<bool> InitializeMicrophoneAsync()
        {
            // Initialize the microphone (WaveInEvent)
            _waveIn = new WaveInEvent();
            _waveIn.DataAvailable += OnDataAvailable;
            return await Task.FromResult(true);
        }

        public async Task StartCaptureAsync()
        {
            if (_waveIn == null)
            {
                throw new InvalidOperationException("Microphone not initialized.");
            }

            _waveIn.StartRecording();
            _isCapturing = true;
            await Task.CompletedTask;
        }

        public async Task StopCaptureAsync()
        {
            if (_waveIn == null)
            {
                throw new InvalidOperationException("Microphone not initialized.");
            }

            _waveIn.StopRecording();
            _isCapturing = false;
            await Task.CompletedTask;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            AudioDataCaptured?.Invoke(this, e.Buffer);
        }
    }
}