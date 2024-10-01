using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IWhisperModelService
    {
        Task DownloadModelAsync(string modelName);
        Task<bool> LoadModelAsync(string modelName);
        Task<string> TranscribeAsync(byte[] audioData);
        bool IsModelLoaded();
    }
}