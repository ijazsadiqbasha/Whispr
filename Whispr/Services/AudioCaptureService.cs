using NAudio.Wave;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public class AudioCaptureService : IAudioCaptureService
    {
        private WaveInEvent? _waveIn;
        private byte[] _audioBuffer;
        private int _bufferPosition;
        private bool _isCapturing;
        private readonly WaveFormat _waveFormat;
        private const int MAX_RECORDING_TIME_SECONDS = 60;

        public event EventHandler<byte[]>? AudioDataCaptured;
        public event EventHandler<float>? AudioLevelChanged;

        public bool IsCapturing => _isCapturing;

        public AudioCaptureService()
        {
            _waveFormat = new WaveFormat(16000, 16, 1);
            _audioBuffer = new byte[_waveFormat.AverageBytesPerSecond * MAX_RECORDING_TIME_SECONDS];
        }

        public Task<bool> InitializeMicrophoneAsync()
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = _waveFormat,
                BufferMilliseconds = 50
            };
            _waveIn.DataAvailable += OnDataAvailable;
            return Task.FromResult(true);
        }

        public Task StartCaptureAsync()
        {
            if (_waveIn == null)
            {
                throw new InvalidOperationException("Microphone not initialized.");
            }

            _bufferPosition = 0;
            _waveIn.StartRecording();
            _isCapturing = true;
            return Task.CompletedTask;
        }

        public Task StopCaptureAsync()
        {
            if (_waveIn == null)
            {
                throw new InvalidOperationException("Microphone not initialized.");
            }

            _waveIn.StopRecording();
            _isCapturing = false;
            var wavData = CreateWavFile(_audioBuffer.AsSpan(0, _bufferPosition));
            AudioDataCaptured?.Invoke(this, wavData);
            return Task.CompletedTask;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            float max = 0;
            var span = e.Buffer.AsSpan(0, e.BytesRecorded);
            for (int i = 0; i < span.Length; i += 2)
            {
                short sample = (short)((span[i + 1] << 8) | span[i]);
                var sample32 = Math.Abs(sample / 32768f);
                if (sample32 > max) max = sample32;
            }
            AudioLevelChanged?.Invoke(this, max);

            span.CopyTo(_audioBuffer.AsSpan(_bufferPosition));
            _bufferPosition += e.BytesRecorded;
        }

        private byte[] CreateWavFile(ReadOnlySpan<byte> audioData)
        {
            var headerSize = 44;
            var totalSize = headerSize + audioData.Length;
            var result = new byte[totalSize];
            var resultSpan = result.AsSpan();

            // Write WAV header
            WriteString(resultSpan.Slice(0, 4), "RIFF");
            Write32BitLittleEndian(resultSpan.Slice(4, 4), totalSize - 8);
            WriteString(resultSpan.Slice(8, 4), "WAVE");
            WriteString(resultSpan.Slice(12, 4), "fmt ");
            Write32BitLittleEndian(resultSpan.Slice(16, 4), 16);
            Write16BitLittleEndian(resultSpan.Slice(20, 2), 1);
            Write16BitLittleEndian(resultSpan.Slice(22, 2), (short)_waveFormat.Channels);
            Write32BitLittleEndian(resultSpan.Slice(24, 4), _waveFormat.SampleRate);
            Write32BitLittleEndian(resultSpan.Slice(28, 4), _waveFormat.AverageBytesPerSecond);
            Write16BitLittleEndian(resultSpan.Slice(32, 2), (short)_waveFormat.BlockAlign);
            Write16BitLittleEndian(resultSpan.Slice(34, 2), (short)_waveFormat.BitsPerSample);
            WriteString(resultSpan.Slice(36, 4), "data");
            Write32BitLittleEndian(resultSpan.Slice(40, 4), audioData.Length);

            // Write audio data
            audioData.CopyTo(resultSpan.Slice(headerSize));

            return result;
        }

        private static void WriteString(Span<byte> span, string value) =>
            System.Text.Encoding.ASCII.GetBytes(value).CopyTo(span);

        private static void Write32BitLittleEndian(Span<byte> span, int value)
        {
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
            span[2] = (byte)(value >> 16);
            span[3] = (byte)(value >> 24);
        }

        private static void Write16BitLittleEndian(Span<byte> span, short value)
        {
            span[0] = (byte)value;
            span[1] = (byte)(value >> 8);
        }

        public void Dispose()
        {
            _waveIn?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}