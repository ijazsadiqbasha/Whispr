using System;
using System.Threading.Tasks;

namespace Whispr.Services
{
    public interface IPythonInstallationService
    {
        Task<bool> CheckPythonInstallationAsync();
        Task DownloadPythonAsync(IProgress<double> progress);
        Task ExtractPythonAsync(IProgress<double> progress);
        string[] GetRequiredPackages();
        string GetPythonPath();
        Task InstallPackagesAsync(IProgress<double> progress, Action<string> updateStatus);
        Task<bool> IsPythonInstalledAsync();
        Task SetupPipAsync(IProgress<double> progress);
        Task<bool> VerifyPythonInstallationAsync();
    }
}