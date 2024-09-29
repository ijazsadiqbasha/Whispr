using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IWhisperModelService
    {
        Task DownloadAndConvertModelAsync(string modelName);
        Task LoadModelAsync(string modelName);
        Task<string> TranscribeAsync(byte[] audioData);
    }
}