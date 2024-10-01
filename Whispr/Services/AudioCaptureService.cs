using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public class AudioCaptureService : IAudioCaptureService
    {
        private WaveInEvent? _waveIn;
        private readonly List<byte> _audioBuffer = new List<byte>();
        private bool _isCapturing;
        private readonly WaveFormat _waveFormat;

        public event EventHandler<byte[]>? AudioDataCaptured;
        public event EventHandler<float>? AudioLevelChanged;

        public bool IsCapturing => _isCapturing;

        public AudioCaptureService()
        {
            _waveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono
        }

        public async Task<bool> InitializeMicrophoneAsync()
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = _waveFormat
            };
            _waveIn.DataAvailable += OnDataAvailable;
            return await Task.FromResult(true);
        }

        public async Task StartCaptureAsync()
        {
            if (_waveIn == null)
            {
                throw new InvalidOperationException("Microphone not initialized.");
            }

            _audioBuffer.Clear();
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
            var wavData = CreateWavFile(_audioBuffer.ToArray());
            AudioDataCaptured?.Invoke(this, wavData);
            await Task.CompletedTask;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            float max = 0;
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                var sample32 = sample / 32768f;
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }
            AudioLevelChanged?.Invoke(this, max);

            _audioBuffer.AddRange(new ReadOnlySpan<byte>(e.Buffer, 0, e.BytesRecorded).ToArray());
        }

        private byte[] CreateWavFile(byte[] audioData)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            // Write WAV header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + audioData.Length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)_waveFormat.Channels);
            writer.Write(_waveFormat.SampleRate);
            writer.Write(_waveFormat.AverageBytesPerSecond);
            writer.Write((short)_waveFormat.BlockAlign);
            writer.Write((short)_waveFormat.BitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(audioData.Length);

            // Write audio data
            writer.Write(audioData);
            return ms.ToArray();
        }

        public void Dispose()
        {
            _waveIn?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}