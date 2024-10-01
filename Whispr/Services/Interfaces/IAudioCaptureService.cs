using System;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IAudioCaptureService : IDisposable
    {
        Task StartCaptureAsync();
        Task StopCaptureAsync();
        event EventHandler<byte[]> AudioDataCaptured;
        event EventHandler<float> AudioLevelChanged;
        bool IsCapturing { get; }
        Task<bool> InitializeMicrophoneAsync();
    }
}