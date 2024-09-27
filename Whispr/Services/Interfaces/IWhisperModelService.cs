using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IWhisperModelService
    {
        Task LoadModelAsync(string modelPath);
        Task UnloadModelAsync();
        Task<string> PerformInferenceAsync(byte[] audioData);
        bool IsModelLoaded { get; }
    }
}