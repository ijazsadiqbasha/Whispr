using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IPythonRuntimeService
    {
        Task InitializePythonEnvironmentAsync();
        Task<string> ExecuteScriptAsync(string script);
        bool IsPythonInitialized { get; }
    }
}