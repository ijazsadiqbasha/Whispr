using System;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IAudioCaptureService
    {
        Task StartCaptureAsync();
        Task StopCaptureAsync();
        event EventHandler<byte[]> AudioDataCaptured;
        bool IsCapturing { get; }
        Task<bool> InitializeMicrophoneAsync();
    }
}