using System;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface ITranscriptionService
    {
        Task StartTranscriptionAsync();
        Task StopTranscriptionAsync();
        event EventHandler<string> TranscriptionCompleted;
        bool IsTranscribing { get; }
    }
}