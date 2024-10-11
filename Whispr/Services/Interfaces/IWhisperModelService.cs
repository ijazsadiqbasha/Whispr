using System;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IWhisperModelService
    {
        Task<bool> LoadModelAsync();
        Task<string> TranscribeAsync(byte[] audioData, Action<int> progressCallback);
        bool IsModelLoaded();
    }
}